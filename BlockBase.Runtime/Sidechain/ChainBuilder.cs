
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Protos;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain.Helpers;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;
using static BlockBase.Network.Rounting.MessageForwarder;
using EosSharp.Core.Exceptions;

namespace BlockBase.Runtime.Sidechain
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
        private List<Block> _blocksApproved;
        private List<Block> _orphanBlocks;
        private List<int> _missingBlocksSequenceNumber;
        private Block _lastValidSavedBlock;
        private BlockheaderTable _lastSidechainBlockheader;
        private ISidechainDatabasesManager _sidechainDatabaseManager;
        private bool _receiving;
        private DateTime _lastReceivedDate;
        private ProducerInPool _currentSendingProducer;

        private static readonly int MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS = 2;
        private static readonly int SLICE_SIZE = 3;

        public ChainBuilder(ILogger logger, SidechainPool sidechainPool, IMongoDbProducerService mongoDbProducerService, ISidechainDatabasesManager sidechainDatabaseManager, NodeConfigurations nodeConfigurations, INetworkService networkService, IMainchainService mainchainService, string endPoint)
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
            _sidechainDatabaseManager = sidechainDatabaseManager;
        }

        public TaskContainer Start(SidechainPool sidechainPool)
        {
            _sidechainPool = sidechainPool;
            TaskContainer = TaskContainer.Create(async () => await Execute());
            TaskContainer.Start();
            return TaskContainer;
        }

        public async Task Execute()
        {
            await _mongoDbProducerService.RemoveUnconfirmedBlocks(_sidechainPool.ClientAccountName);
            var producerIndex = 0;
            var validConnectedProducers = _sidechainPool.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected).ToList();
            if (!validConnectedProducers.Any())
            {
                _logger.LogDebug("No connected producers to request blocks.");
                return;
            }
            _currentSendingProducer = validConnectedProducers.ElementAt(producerIndex);

            while (true)
            {
                _missingBlocksSequenceNumber = (await GetSequenceNumberOfMissingBlocks()).Take(SLICE_SIZE).ToList();
                if (_missingBlocksSequenceNumber.Count() == 0)
                {
                    try
                    {
                        await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
                        _logger.LogDebug("Notified ready.");
                    }
                    catch (ApiErrorException)
                    {
                        _logger.LogInformation("Already notified ready.");
                    }
                    return;
                }

                _receiving = true;

                _lastValidSavedBlock = await _mongoDbProducerService.GetLastValidSidechainBlockAsync(_sidechainPool.ClientAccountName);

                _logger.LogDebug($"Asking for blocks:");
                foreach (var missingSequenceNumber in _missingBlocksSequenceNumber) _logger.LogDebug(missingSequenceNumber + "");

                var message = BuildRequestBlocksNetworkMessage(_currentSendingProducer, _missingBlocksSequenceNumber, _sidechainPool.ClientAccountName);
                await _networkService.SendMessageAsync(message);

                _lastReceivedDate = DateTime.UtcNow;
                while (_receiving && DateTime.UtcNow.Subtract(_lastReceivedDate).TotalSeconds <= MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS)
                {
                    await Task.Delay(MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS * 1000);
                }

                if (_receiving && DateTime.UtcNow.Subtract(_lastReceivedDate).TotalSeconds > MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS)
                {
                    _logger.LogDebug("Too much time without receiving block. Asking another producer for remaining blocks.");
                    producerIndex = !validConnectedProducers.Contains(_currentSendingProducer) ? 0 : validConnectedProducers.IndexOf(_currentSendingProducer) + 1;
                    if (producerIndex > validConnectedProducers.Count() - 1)
                    {
                        _logger.LogDebug("Tried all producers and didn't manage to build chain, trying again later...");
                        return;
                    }
                }
            }
        }

        public NetworkMessage BuildRequestBlocksNetworkMessage(ProducerInPool producer, IList<int> missingSequenceNumbers, string sidechainPoolName)
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

        private async void MessageForwarder_RecoverBlockReceived(BlockReceivedEventArgs args, IPEndPoint sender)
        {
            if (!_receiving ||
            !_currentSendingProducer.PeerConnection.IPEndPoint.Address.Equals(sender.Address)
            || _currentSendingProducer.PeerConnection.IPEndPoint.Port != sender.Port)
                return;

            _lastReceivedDate = DateTime.UtcNow;

            _logger.LogDebug("Block from sidechain: " + args.ClientAccountName);
            if (args.ClientAccountName != _sidechainPool.ClientAccountName) return;

            var blockProto = SerializationHelper.DeserializeBlock(args.BlockBytes, _logger);
            _logger.LogDebug($"Received block {blockProto.BlockHeader.SequenceNumber}.");
            if (blockProto == null) return;

            await HandleReceivedBlock(blockProto);
        }


        public async Task HandleReceivedBlock(BlockProto blockProtoReceived)
        {
            var blockReceived = new Block().SetValuesFromProto(blockProtoReceived);
            if (!ValidationHelper.IsBlockHashValid(blockReceived.BlockHeader, out byte[] trueBlockHash))
            {
                _logger.LogDebug("Blockhash not valid.");
                return;
            }

            if ((await _mongoDbProducerService.GetSidechainBlockAsync(_sidechainPool.ClientAccountName, HashHelper.ByteArrayToFormattedHexaString(blockReceived.BlockHeader.BlockHash))) != null
                || _blocksApproved.Where(b => b.BlockHeader.BlockHash.SequenceEqual(blockReceived.BlockHeader.BlockHash)).SingleOrDefault() != null)
            {
                _logger.LogDebug("Block already saved.");
                return;
            }

            var blockHeaderSC = _lastSidechainBlockheader.ConvertToBlockHeader();
            if (blockReceived.BlockHeader.SequenceNumber == blockHeaderSC.SequenceNumber)
            {
                if (ValidationHelper.ValidateBlockAndBlockheader(blockReceived, _sidechainPool, blockHeaderSC, _logger, out byte[] blockHash))
                    AddApprovedBlock(blockReceived);
            }
            else
            {
                var blockAfter = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(_sidechainPool.ClientAccountName, blockReceived.BlockHeader.SequenceNumber + 1, blockReceived.BlockHeader.SequenceNumber + 1)).SingleOrDefault();
                if (blockAfter == null) blockAfter = _blocksApproved.Where(b => b.BlockHeader.SequenceNumber == blockReceived.BlockHeader.SequenceNumber + 1).SingleOrDefault();

                if (blockAfter == null && _missingBlocksSequenceNumber.Contains((int)blockReceived.BlockHeader.SequenceNumber))
                    _orphanBlocks.Add(blockReceived);

                else if (blockAfter.BlockHeader.PreviousBlockHash.SequenceEqual(blockReceived.BlockHeader.BlockHash))
                    AddApprovedBlock(blockReceived);
            }
            if (_missingBlocksSequenceNumber.OrderByDescending(n => n).SequenceEqual(_blocksApproved.Select(b => (int)(b.BlockHeader.SequenceNumber)).OrderByDescending(n => n)))
            {
                await UpdateDatabase();
                _receiving = false;
            }
        }

        private void AddApprovedBlock(Block block)
        {
            if (_blocksApproved.Select(o => o.BlockHeader.BlockHash == block.BlockHeader.BlockHash).Count() != 0)
            {
                _blocksApproved.Add(block);
                foreach (var orphan in _orphanBlocks)
                {
                    if (orphan.BlockHeader.SequenceNumber + 1 == block.BlockHeader.SequenceNumber
                    && orphan.BlockHeader.BlockHash.SequenceEqual(block.BlockHeader.PreviousBlockHash))
                        _blocksApproved.Add(orphan);
                }
            }
        }

        private async Task UpdateDatabase()
        {
            var orderedBlocks = _blocksApproved.OrderBy(b => b.BlockHeader.SequenceNumber);
            var databaseName = _sidechainPool.ClientAccountName;

            foreach (Block block in orderedBlocks)
            {
                _logger.LogDebug($"Adding block #{block.BlockHeader.SequenceNumber} to database.");
                _lastReceivedDate = DateTime.UtcNow;
                await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(block, databaseName);
                var transactions = await _mongoDbProducerService.GetBlockTransactionsAsync(_sidechainPool.ClientAccountName, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
                //_sidechainDatabaseManager.ExecuteBlockTransactions(transactions);
                await _mongoDbProducerService.ConfirmBlock(databaseName, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
            }
            _blocksApproved.Clear();
            _logger.LogDebug("Added blocks to database.");
        }



        private async Task<IList<int>> GetSequenceNumberOfMissingBlocks()
        {
            var missingSequenceNumbers = new List<int>();
            _lastSidechainBlockheader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            var savedBlocks = await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(_sidechainPool.ClientAccountName, 1, _lastSidechainBlockheader.SequenceNumber);
            missingSequenceNumbers = Enumerable.Range(1, Convert.ToInt32(_lastSidechainBlockheader.SequenceNumber)).Except(savedBlocks.Select(b => Convert.ToInt32(b.BlockHeader.SequenceNumber))).ToList();

            return missingSequenceNumbers.OrderByDescending(s => s).ToList();
        }
    }
}