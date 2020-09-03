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
        private readonly int MAX_BYTES_PER_MESSAGE = 250000;
        private readonly int WAIT_TIME_IN_SECONDS = 20;
        private ThreadSafeList<TransactionSendingTrackPoco> _transactionsToSend;
        private IMainchainService _mainchainService;
        private ThreadSafeList<ProducerConfirmationPoco> _currentProducers;
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
            _currentProducers = new ThreadSafeList<ProducerConfirmationPoco>();
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

        private async void MessageForwarder_TransactionConfirmationReceived(MessageForwarder.TransactionConfirmationReceivedEventArgs args, IPEndPoint sender)
        {
            foreach (var sequenceNumber in args.TransactionSequenceNumbers)
            {
                var transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber == sequenceNumber).SingleOrDefault();
                var producer = _currentProducers.GetEnumerable().Where(p => p.Producer == args.SenderAccountName).SingleOrDefault();
                if (transactionSendingTrackPoco != null && producer != null)
                {
                    transactionSendingTrackPoco.ProducersAlreadyReceived.Add(args.SenderAccountName);
                    producer.LastConfirmationReceivedDateTime = DateTime.UtcNow;
                }
            }

            var transactionToSend = _transactionsToSend.GetEnumerable().Where(p => !p.ProducersAlreadyReceived.GetEnumerable().Contains(args.SenderAccountName)).Select(p => p.Transaction);
            if (transactionToSend.Count() != 0)
                await SendScriptTransactionsToProducer(transactionToSend, args.SenderAccountName, sender);
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

        // marciak - removes already included transactions from database and list to send
        public async Task RemoveIncludedTransactions(uint numberOfIncludedTransactions, ulong lastIncludedTransactionSequenceNumber)
        {
            if (numberOfIncludedTransactions == 0) _numberOfConsecutiveEmptyBlocks++;
            else _numberOfConsecutiveEmptyBlocks = 0;

            await _mongoDbRequesterService.RemoveAlreadyIncludedTransactionsDBAsync(_nodeConfigurations.AccountName, lastIncludedTransactionSequenceNumber);

            var transactionSendingTrackPocosToRemove = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber <= lastIncludedTransactionSequenceNumber).ToList();
            foreach (var transactionSendingTrackPocoToRemove in transactionSendingTrackPocosToRemove) _transactionsToSend.Remove(transactionSendingTrackPocoToRemove);
        }

        public async Task RollbackWaitingTransactions(uint numberOfIncludedTransactions, ulong lastIncludedTransactionSequenceNumber)
        {
            var transactionsToResend = await _mongoDbRequesterService.RollbackAndRetrieveWaitingTransactions(_nodeConfigurations.AccountName, lastIncludedTransactionSequenceNumber);

            foreach (var transaction in transactionsToResend)
                AddScriptTransactionToSend(transaction);
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
                        foreach (var producer in producers)
                        {
                            if (!_currentProducers.GetEnumerable().Any(p => p.Producer == producer.Key))
                                _currentProducers.Add(new ProducerConfirmationPoco() { Producer = producer.Key, LastConfirmationReceivedDateTime = DateTime.UtcNow });
                        }
                        ClearProducersThatLeft(producers);
                        await TryToSendTransactions();
                    }

                    await Task.Delay(WAIT_TIME_IN_SECONDS * 1000);

                }
                catch (Exception e)
                {
                    _logger.LogCritical($"Send transactions failed with: {e}");
                }
            }
        }

        private void ClearProducersThatLeft(List<ProducerInTable> producers)
        {
            var producersToRemove = new List<ProducerConfirmationPoco>();

            foreach (var producer in _currentProducers)
            {
                if (!producers.Any(p => p.Key == producer.Producer))
                    producersToRemove.Add(producer);
            }

            foreach (var producerToRemove in producersToRemove)
            {
                _currentProducers.Remove(producerToRemove);
            }
        }

        private async Task TryToSendTransactions()
        {
            var listPeerConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().ToList();
            ListHelper.Shuffle<PeerConnection>(listPeerConnections);
            var sendTasks = new List<Task>();
            foreach (var peerConnection in listPeerConnections)
            {
                if (peerConnection.ConnectionState == ConnectionStateEnum.Connected && _currentProducers.GetEnumerable().Any(p => p.Producer == peerConnection.ConnectionAccountName && p.LastConfirmationReceivedDateTime.AddSeconds(WAIT_TIME_IN_SECONDS) <= DateTime.UtcNow))
                {
                    var transactionToSend = _transactionsToSend.GetEnumerable().Where(p => !p.ProducersAlreadyReceived.GetEnumerable().Contains(peerConnection.ConnectionAccountName)).Select(p => p.Transaction);
                    if (transactionToSend.Count() != 0)
                        sendTasks.Add(SendScriptTransactionsToProducer(transactionToSend, peerConnection.ConnectionAccountName, peerConnection.IPEndPoint));
                }
            }
            await Task.WhenAll(sendTasks);
        }

        private async Task SendScriptTransactionsToProducer(IEnumerable<Transaction> transactions, string producer, IPEndPoint endpointToSend)
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
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransactions, data.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.GetResolvedIp() + ":" + _networkConfigurations.TcpPort, _nodeConfigurations.AccountName, endpointToSend);

            _logger.LogDebug($"Sending transactions #{transactions?.First()?.SequenceNumber} to #{transactions?.Last()?.SequenceNumber} to producer {producer}");
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