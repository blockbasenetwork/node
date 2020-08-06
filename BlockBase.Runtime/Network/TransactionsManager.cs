using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Network;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Rounting;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Network
{
    public class TransactionsManager
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        private TaskContainer TaskContainer;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NetworkConfigurations _networkConfigurations;
        private readonly int MAX_BYTES_PER_MESSAGE = 500000;
        private readonly int WAIT_TIME_IN_SECONDS = 15;
        private ThreadSafeList<TransactionSendingTrackPoco> _transactionsToSend;
        private IMainchainService _mainchainService;
        private ThreadSafeList<ProducerInTable> _currentProducers;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private int _numberOfConsecutiveEmptyBlocks;
        private bool _hasBeenSetup = false;

        public TransactionsManager(ILogger<TransactionsManager> logger, IOptions<NodeConfigurations> nodeConfigurations, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IOptions<NetworkConfigurations> networkConfigurations, IMainchainService mainchainService, IMongoDbRequesterService mongoDbRequesterService)
        {
            _networkService = networkService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations.Value;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations.Value;
            _transactionsToSend = new ThreadSafeList<TransactionSendingTrackPoco>();
            _currentProducers = new ThreadSafeList<ProducerInTable>();
            _mainchainService = mainchainService;
            _mongoDbRequesterService = mongoDbRequesterService;
            _networkService.SubscribeTransactionConfirmationReceivedEvent(MessageForwarder_TransactionConfirmationReceived);

            _numberOfConsecutiveEmptyBlocks = 0;

        }

        public async Task Setup()
        {
            if (!_hasBeenSetup)
            {
                await LoadTransactionsFromDatabase();
                _hasBeenSetup = true;
            }
        }

        private void MessageForwarder_TransactionConfirmationReceived(MessageForwarder.TransactionConfirmationReceivedEventArgs args, IPEndPoint sender)
        {
            foreach (var sequenceNumber in args.TransactionSequenceNumbers)
            {
                var transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber == sequenceNumber).SingleOrDefault();
                if (transactionSendingTrackPoco != null)
                {
                    if (_currentProducers.GetEnumerable().Where(p => p.Key == args.SenderAccountName).Count() != 0)
                    {
                        transactionSendingTrackPoco.ProducersAlreadyReceived.Add(args.SenderAccountName);
                    }
                }
            }
        }

        public TaskContainer Start()
        {
            _logger.LogDebug("Task starting.");

            if (TaskContainer != null) TaskContainer.Stop();

            TaskContainer = TaskContainer.Create(Execute);
            TaskContainer.Start();
            return TaskContainer;
        }

        public void Stop()
        {
            if (TaskContainer != null && TaskContainer.IsRunning())
            {
                TaskContainer.Stop();
            }
        }

        public async Task RemoveIncludedTransactions(uint numberOfIncludedTransactions, ulong lastIncludedTransactionSequenceNumber)
        {
            if (numberOfIncludedTransactions == 0) _numberOfConsecutiveEmptyBlocks++;
            else _numberOfConsecutiveEmptyBlocks = 0;

            await _mongoDbRequesterService.RemoveAlreadyIncludedTransactionsDBAsync(_nodeConfigurations.AccountName, lastIncludedTransactionSequenceNumber);

            var transactionSendingTrackPocosToRemove = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber <= lastIncludedTransactionSequenceNumber).ToList();
            foreach (var transactionSendingTrackPocoToRemove in transactionSendingTrackPocosToRemove) _transactionsToSend.Remove(transactionSendingTrackPocoToRemove);
        }
        public void AddScriptTransactionToSend(Transaction transaction)
        {
            if (_transactionsToSend.GetEnumerable().Any(t => t.Transaction.TransactionHash.SequenceEqual(transaction.TransactionHash))) return;

            TransactionSendingTrackPoco transactionSendingTrack;

            transactionSendingTrack = new TransactionSendingTrackPoco()
            {
                Transaction = transaction,
                ProducersAlreadyReceived = new ThreadSafeList<string>()
            };

            _transactionsToSend.Add(transactionSendingTrack);
        }

        private async Task Execute()
        {
            while (true)
            {
                try
                {
                    if (_transactionsToSend.Count() != 0)
                    {
                        var producers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
                        _currentProducers.ClearAndAddRange(producers);
                        await TryToSendTransactions(producers);
                    }
                    
                    await Task.Delay(_transactionsToSend.Count() > 1000 ? 100000000 : WAIT_TIME_IN_SECONDS * 1000);

                }
                catch (Exception e)
                {
                    _logger.LogCritical($"Send transactions failed with: {e}");
                }
            }
        }

        private async Task TryToSendTransactions(IEnumerable<ProducerInTable> producers)
        {
            var listPeerConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().ToList();
            ListHelper.Shuffle<PeerConnection>(listPeerConnections);
            var sendTasks = new List<Task>();
            foreach (var peerConnection in listPeerConnections)
            {
                if (peerConnection.ConnectionState == ConnectionStateEnum.Connected)
                {
                    var transactionToSend = _transactionsToSend.GetEnumerable().Where(p => !p.ProducersAlreadyReceived.GetEnumerable().Contains(peerConnection.ConnectionAccountName)).Select(p => p.Transaction);
                    if (transactionToSend.Count() != 0)
                        sendTasks.Add(SendScriptTransactionsToProducer(transactionToSend, peerConnection));
                }
            }
            await Task.WhenAll(sendTasks);
        }

        private async Task SendScriptTransactionsToProducer(IEnumerable<Transaction> transactions, PeerConnection peerConnection)
        {
            var data = new List<byte>();
            var sidechainNameBytes = Encoding.UTF8.GetBytes(_nodeConfigurations.AccountName);
            short lenght = (short)sidechainNameBytes.Length;
            data.AddRange(BitConverter.GetBytes(lenght));
            data.AddRange(sidechainNameBytes);
            foreach (var transaction in transactions)
            {
                var transactionBytes = transaction.ConvertToProto().ToByteArray();
                data.AddRange(BitConverter.GetBytes(transactionBytes.Count()));
                data.AddRange(transactionBytes);
                if (data.Count >= MAX_BYTES_PER_MESSAGE) break;
            }
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransactions, data.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.GetResolvedIp() + ":" + _networkConfigurations.TcpPort, _nodeConfigurations.AccountName, peerConnection.IPEndPoint);

            _logger.LogDebug($"Sending transactions #{transactions?.First()?.SequenceNumber} to #{transactions?.Last()?.SequenceNumber} to producer {peerConnection.ConnectionAccountName}");
            await _networkService.SendMessageAsync(message);
        }

        private async Task LoadTransactionsFromDatabase()
        {
            try
            {
                var transactions = await _mongoDbRequesterService.RetrieveTransactionsInMempool(_nodeConfigurations.AccountName);
                foreach (var transaction in transactions)
                    AddScriptTransactionToSend(transaction);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.Message, "Unable to connect to mongodb database");
            }
        }
    }
}