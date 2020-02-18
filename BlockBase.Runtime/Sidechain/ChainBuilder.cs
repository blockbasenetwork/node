
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
        private List<Block> _blocksReceived;
        private Block _lastValidSavedBlock;
        private BlockheaderTable _lastSidechainBlockheader;
        private ISidechainDatabasesManager _sidechainDatabaseManager;
        private bool _completed;
        private bool _receiving;
        private DateTime _lastReceivedDate;

        private static readonly object Locker = new object();
        private static readonly int MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS = 120;

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
            _blocksReceived = new List<Block>();
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
            if (_receiving) return;
            _completed = false;
            _receiving = true;
            var currentSendingProducer = new ProducerInPool();
            var databaseName = _sidechainPool.ClientAccountName;

            while (!_completed)
            {
                var validConnectedProducers = _sidechainPool.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected).ToList();
                if (!validConnectedProducers.Any())
                {
                    _logger.LogDebug("No connected producers to request blocks");
                    return;
                }
                if (currentSendingProducer == validConnectedProducers.Last())
                {
                    _logger.LogDebug("Tried all producers and didn't manage to build chain, trying again later...");
                    return;
                }

                await _mongoDbProducerService.RemoveUnconfirmedBlocks(databaseName);
                _lastValidSavedBlock = await _mongoDbProducerService.GetLastValidSidechainBlockAsync(databaseName);
                _lastSidechainBlockheader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

                var selectedProducerToSend = !validConnectedProducers.Contains(currentSendingProducer) ? 0 : validConnectedProducers.IndexOf(currentSendingProducer) + 1;
                currentSendingProducer = validConnectedProducers.ElementAt(selectedProducerToSend);

                var beginSequenceNumber = _lastValidSavedBlock != null ? _lastValidSavedBlock.BlockHeader.SequenceNumber : 0;
                var endSequenceNumber = _blocksReceived.Any() ?
                    _blocksReceived.OrderBy(b => b.BlockHeader.SequenceNumber).Last().BlockHeader.SequenceNumber :
                    _lastSidechainBlockheader.SequenceNumber;

                _logger.LogDebug($"Asking for blocks {beginSequenceNumber} to {endSequenceNumber}");

                var message = BuildRequestBlocksNetworkMessage(currentSendingProducer, beginSequenceNumber, endSequenceNumber, _sidechainPool.ClientAccountName);
                await _networkService.SendMessageAsync(message);

                _lastReceivedDate = DateTime.UtcNow;
                while (_receiving && DateTime.UtcNow.Subtract(_lastReceivedDate).TotalSeconds <= MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS)
                {
                    await Task.Delay(MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS * 100);
                }

                if (_receiving && DateTime.UtcNow.Subtract(_lastReceivedDate).TotalSeconds > MAX_TIME_BETWEEN_BLOCKS_IN_SECONDS)
                {
                    _logger.LogDebug("Too much time without receiving block. Asking another producer for remaining blocks.");
                    _receiving = false;
                }
            }
        }

        public NetworkMessage BuildRequestBlocksNetworkMessage(ProducerInPool producer, ulong beginSequenceNumber, ulong endSequenceNumber, string sidechainPoolName)
        {
            var beginSequenceNumberBytes = BitConverter.GetBytes(beginSequenceNumber);

            var endSequenceNumberBytes = BitConverter.GetBytes(endSequenceNumber);

            var sidechainNameBytes = Encoding.UTF8.GetBytes(sidechainPoolName);

            var data = beginSequenceNumberBytes.Concat(endSequenceNumberBytes).Concat(sidechainNameBytes).ToArray();

            _logger.LogDebug(_nodeConfigurations.ActivePrivateKey);

            return new NetworkMessage(NetworkMessageTypeEnum.RequestBlocks, data, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, producer.PeerConnection.IPEndPoint);
        }

        private async void MessageForwarder_RecoverBlockReceived(BlockReceivedEventArgs args, IPEndPoint sender)
        {
            if (!_receiving) return;

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
            var valid = false;

            var blockReceived = new Block().SetValuesFromProto(blockProtoReceived);

            lock (Locker)
            {
                if (_blocksReceived.Count() == 0)
                {
                    var blockHeaderSC = _lastSidechainBlockheader.ConvertToBlockHeader();

                    if (ValidationHelper.ValidateBlockAndBlockheader(blockReceived, _sidechainPool, blockHeaderSC, _logger, out byte[] trueBlockHash)) valid = true;
                }
                else
                {
                    var lastReceivedBlock = _blocksReceived.Last();

                    if (blockReceived.BlockHeader.SequenceNumber == lastReceivedBlock.BlockHeader.SequenceNumber - 1
                        && lastReceivedBlock.BlockHeader.PreviousBlockHash.SequenceEqual(blockReceived.BlockHeader.BlockHash))
                    {
                        _logger.LogDebug("Blockhash and sequence number are right.");
                        if (ValidationHelper.IsBlockHashValid(blockReceived.BlockHeader, out byte[] trueBlockHash)) valid = true;
                    }
                }
                if (valid)
                {
                    _logger.LogDebug("Block Received is valid.");
                    _blocksReceived.Add(blockReceived);
                }
            }
            var sequenceNumber = _lastValidSavedBlock != null ? _lastValidSavedBlock.BlockHeader.SequenceNumber : 0;
            if (valid && blockReceived.BlockHeader.SequenceNumber == sequenceNumber + 1)
            {
                _receiving = false;
                await UpdateDatabase();
            }
        }

        private async Task UpdateDatabase()
        {
            _logger.LogDebug("Adding blocks to database.");
            var orderedBlocks = _blocksReceived.OrderBy(b => b.BlockHeader.SequenceNumber);
            var databaseName = _sidechainPool.ClientAccountName;

            foreach (Block block in orderedBlocks)
            {
                _lastReceivedDate = DateTime.UtcNow;
                await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(block, databaseName);
                var transactions = await _mongoDbProducerService.GetBlockTransactionsAsync(_sidechainPool.ClientAccountName, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
                //_sidechainDatabaseManager.ExecuteBlockTransactions(transactions);
                await _mongoDbProducerService.ConfirmBlock(databaseName, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
            }
            _blocksReceived.Clear();
            _receiving = false;
            _completed = true;

            try
            {
                await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
            }
            catch (ApiErrorException)
            {
                _logger.LogInformation("Already notified ready.");
            }
        }
    }
}