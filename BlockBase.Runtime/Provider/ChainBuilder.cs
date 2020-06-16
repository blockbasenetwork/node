
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;
using static BlockBase.Network.Rounting.MessageForwarder;
using EosSharp.Core.Exceptions;
using BlockBase.Utils.Operation;
using BlockBase.Runtime.Helpers;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Protos;

namespace BlockBase.Runtime.Provider
{
    // public class ChainBuilder : IThreadableComponent
    public class ChainBuilder
    {
        public TaskContainer TaskContainer { get; private set; }

        private SidechainPool _sidechainPool;
        private IMongoDbProducerService _mongoDbProducerService;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private string _endPoint;
        private IList<Block> _blocksApproved;
        private IList<Block> _orphanBlocks;
        private IList<ulong> _missingBlocksSequenceNumber;
        private IList<ulong> _currentlyGettingBlocks;
        private BlockheaderTable _lastSidechainBlockheader;
        private bool _receiving;
        private DateTime _lastReceivedDate;
        private ProducerInPool _currentSendingProducer;
        TransactionValidationsHandler _transactionValidationsHandler;

        private static readonly int MAX_TIME_BETWEEN_MESSAGES_IN_SECONDS = 10;
        private static readonly int SLICE_SIZE = 40;
        private object locker = new object();


        public ChainBuilder(ILogger logger, SidechainPool sidechainPool, IMongoDbProducerService mongoDbProducerService, NodeConfigurations nodeConfigurations, INetworkService networkService, IMainchainService mainchainService, string endPoint, TransactionValidationsHandler transactionValidationsHandler)
        {
            _logger = logger;
            _sidechainPool = sidechainPool;
            _mongoDbProducerService = mongoDbProducerService;
            _nodeConfigurations = nodeConfigurations;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _endPoint = endPoint;
            _networkService.SubscribeRecoverBlockReceivedEvent(MessageForwarder_RecoverBlockReceived);
            _blocksApproved = new List<Block>();
            _orphanBlocks = new List<Block>();

            _transactionValidationsHandler = transactionValidationsHandler;
        }

        public async Task<OpResult<bool>> Run()
        {
            try
            {
                var producerIndex = 0;
                var validConnectedProducers = _sidechainPool.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected).ToList();

                if (!validConnectedProducers.Any())
                {
                    // _logger.LogDebug("No connected producers to request blocks.");
                    return new OpResult<bool>(new Exception("Unable to synchronize. No connected producers to request blocks."));
                }

                _lastSidechainBlockheader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

                if (_sidechainPool.ProducerType == ProducerTypeEnum.Validator)
                    _missingBlocksSequenceNumber = new List<ulong> { _lastSidechainBlockheader.SequenceNumber };

                else
                    _missingBlocksSequenceNumber = (await GetSequenceNumberOfMissingBlocks(_lastSidechainBlockheader.SequenceNumber)).ToList();

                while (true)
                {
                    _currentSendingProducer = validConnectedProducers.ElementAt(producerIndex);

                    if (_missingBlocksSequenceNumber.Count() == 0)
                    {
                        _logger.LogDebug("No more missing blocks.");
                        return new OpResult<bool>(true);
                    }

                    _currentlyGettingBlocks = _missingBlocksSequenceNumber.Take(SLICE_SIZE).ToList();

                    await _mongoDbProducerService.RemoveUnconfirmedBlocks(_sidechainPool.ClientAccountName);

                    _receiving = true;

                    _logger.LogDebug($"Asking for blocks:");
                    foreach (var missingSequenceNumber in _currentlyGettingBlocks) _logger.LogDebug(missingSequenceNumber + "");

                    var blockMessage = BuildRequestBlocksNetworkMessage(_currentSendingProducer, _currentlyGettingBlocks, _sidechainPool.ClientAccountName);
                    await _networkService.SendMessageAsync(blockMessage);

                    _lastReceivedDate = DateTime.UtcNow;
                    while (_receiving && DateTime.UtcNow.Subtract(_lastReceivedDate).TotalSeconds <= MAX_TIME_BETWEEN_MESSAGES_IN_SECONDS)
                    {
                        await Task.Delay(50);
                    }

                    if (_receiving && DateTime.UtcNow.Subtract(_lastReceivedDate).TotalSeconds > MAX_TIME_BETWEEN_MESSAGES_IN_SECONDS)
                    {
                        _logger.LogDebug("Too much time without receiving block. Asking another producer for remaining blocks.");
                        producerIndex = !validConnectedProducers.Contains(_currentSendingProducer) ? 0 : validConnectedProducers.IndexOf(_currentSendingProducer) + 1;
                        if (producerIndex > validConnectedProducers.Count() - 1)
                        {
                            _logger.LogDebug("Tried all producers and didn't manage to build chain, trying again later...");
                            return new OpResult<bool>(new Exception("Tried all producers and didn't manage to build chain, trying again later"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new OpResult<bool>(ex);
            }
        }

        private NetworkMessage BuildRequestBlocksNetworkMessage(ProducerInPool producer, IEnumerable<ulong> missingSequenceNumbers, string sidechainPoolName)
        {
            var payload = new List<byte>();

            var l = missingSequenceNumbers.Count();
            var bt = BitConverter.GetBytes(l);
            payload.AddRange(bt);

            foreach (var missingSequenceNumber in missingSequenceNumbers)
                payload.AddRange(BitConverter.GetBytes(missingSequenceNumber));

            payload.AddRange(Encoding.UTF8.GetBytes(sidechainPoolName));

            _logger.LogDebug("Array size: " + payload.Count);

            return new NetworkMessage(NetworkMessageTypeEnum.RequestBlocks, payload.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, producer.PeerConnection.IPEndPoint);
        }
        private NetworkMessage BuildRequestLastIncludedTransactionNetworkMessage(ProducerInPool producer, string sidechainPoolName)
        {
            return new NetworkMessage(NetworkMessageTypeEnum.RequestLastIncludedTransaction, Encoding.UTF8.GetBytes(sidechainPoolName), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, producer.PeerConnection.IPEndPoint);
        }


        private async void MessageForwarder_RecoverBlockReceived(BlockReceivedEventArgs args, IPEndPoint sender)
        {
            if (!_receiving ||
            !_currentSendingProducer.PeerConnection.IPEndPoint.Address.Equals(sender.Address) ||
            _currentSendingProducer.PeerConnection.IPEndPoint.Port != sender.Port)
                return;

            _lastReceivedDate = DateTime.UtcNow;

            _logger.LogDebug("Blocks from sidechain: " + args.ClientAccountName);
            if (args.ClientAccountName != _sidechainPool.ClientAccountName) return;

            var blockProtos = SerializationHelper.DeserializeBlocks(args.BlockBytes, _logger);
            if (!blockProtos.Any() || blockProtos == null) return;

            _lastSidechainBlockheader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
            var blockHeaderSC = _lastSidechainBlockheader.ConvertToBlockHeader();
            var firstBlockProto = blockProtos.FirstOrDefault();
            var blockAfter = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(_sidechainPool.ClientAccountName, firstBlockProto.BlockHeader.SequenceNumber + 1, firstBlockProto.BlockHeader.SequenceNumber + 1)).SingleOrDefault();
            foreach (var blockProto in blockProtos)
            {
                var blockReceived = new Block().SetValuesFromProto(blockProto);
                await HandleReceivedBlock(blockReceived, blockHeaderSC, blockAfter);
                blockAfter = blockReceived;
            }
        }


        private async Task HandleReceivedBlock(Block blockReceived, BlockHeader blockHeaderFromSC, Block blockAfter)
        {
            if (!_currentlyGettingBlocks.Contains(blockReceived.BlockHeader.SequenceNumber))
            {
                _logger.LogDebug("Block received was not requested.");
                return;
            }

            if (!ValidationHelper.IsBlockHashValid(blockReceived.BlockHeader, out byte[] trueBlockHash))
            {
                _logger.LogDebug("Blockhash not valid.");
                return;
            }

            if ((await _mongoDbProducerService.IsBlockInDatabase(_sidechainPool.ClientAccountName, HashHelper.ByteArrayToFormattedHexaString(blockReceived.BlockHeader.BlockHash))))
            {
                _logger.LogDebug("Block already saved in database.");
                return;
            }

            if (blockReceived.BlockHeader.SequenceNumber == blockHeaderFromSC.SequenceNumber)
            {
                if (ValidationHelper.ValidateBlockAndBlockheader(blockReceived, _sidechainPool, blockHeaderFromSC, _logger, out byte[] blockHash))
                    AddApprovedBlock(blockReceived);
                else
                    _logger.LogDebug("Block is not according to sc block.");
            }
            else
            {
                if (blockAfter == null)
                    blockAfter = _blocksApproved.Where(b => b.BlockHeader.PreviousBlockHash.SequenceEqual(blockReceived.BlockHeader.BlockHash)).SingleOrDefault();

                if (blockAfter == null)
                    AddOrphanBlock(blockReceived);
                else
                    AddApprovedBlock(blockReceived);
            }

            if (_blocksApproved.Count() == _currentlyGettingBlocks.Count())
            {
                await UpdateDatabase();
                _receiving = false;
            }
        }

        private void AddApprovedBlock(Block block)
        {
            if (_blocksApproved.Where(o => o.BlockHeader.BlockHash.SequenceEqual(block.BlockHeader.BlockHash)).Count() == 0)
            {
                _blocksApproved.Add(block);
                _logger.LogDebug($"Added block {block.BlockHeader.SequenceNumber} to approved blocks.");

                var orphan = _orphanBlocks.Where(o => o.BlockHeader.BlockHash.SequenceEqual(block.BlockHeader.PreviousBlockHash)).SingleOrDefault();
                if (orphan != null)
                {
                    _orphanBlocks.Remove(orphan);
                    AddApprovedBlock(orphan);
                    _logger.LogDebug($"Removed block {orphan.BlockHeader.SequenceNumber} from orphan blocks.");
                }
            }
            else
                _logger.LogDebug($"Block {block.BlockHeader.SequenceNumber} already saved in approved blocks.");
        }

        private void AddOrphanBlock(Block block)
        {
            if (_orphanBlocks.Where(o => o.BlockHeader.BlockHash.SequenceEqual(block.BlockHeader.BlockHash)).Count() == 0)
            {
                _orphanBlocks.Add(block);
                _logger.LogDebug($"Added block {block.BlockHeader.SequenceNumber} to orphan blocks.");

            }
            else
                _logger.LogDebug($"Block {block.BlockHeader.SequenceNumber} already saved in orphans.");
        }

        private async Task UpdateDatabase()
        {
            var orderedBlocks = _blocksApproved.OrderBy(b => b.BlockHeader.SequenceNumber);
            var databaseName = _sidechainPool.ClientAccountName;

            foreach (Block block in orderedBlocks)
            {
                try
                {
                    _logger.LogDebug($"Adding block #{block.BlockHeader.SequenceNumber} to database.");
                    _lastReceivedDate = DateTime.UtcNow;
                    await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(block, databaseName);
                    var transactions = await _mongoDbProducerService.GetBlockTransactionsAsync(_sidechainPool.ClientAccountName, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
                    await _mongoDbProducerService.ConfirmBlock(databaseName, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
                    _missingBlocksSequenceNumber = _missingBlocksSequenceNumber.Where(s => s != block.BlockHeader.SequenceNumber).ToList();
                }
                catch (Exception)
                {
                    _logger.LogWarning($"Unable to add #{block.BlockHeader.SequenceNumber} to database.");
                }
            }
            _blocksApproved.Clear();
            _orphanBlocks.Clear();
        }



        private async Task<IEnumerable<ulong>> GetSequenceNumberOfMissingBlocks(uint lastSidechainBlockheaderSequenceNumber)
        {
            return await _mongoDbProducerService.GetMissingBlockNumbers(_sidechainPool.ClientAccountName, lastSidechainBlockheaderSequenceNumber);
        }


    }
}