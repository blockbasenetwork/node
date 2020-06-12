using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class TransactionsHandler
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        public Task Task { get; private set; }
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NetworkConfigurations _networkConfigurations;
        private readonly int MAX_TRANSACTIONS_PER_MESSAGE = 50;
        private readonly int WAIT_TIME_IN_SECONDS = 10;
        private ThreadSafeList<TransactionSendingTrackPoco> _transactionsToSend;
        private IMainchainService _mainchainService;
        private IList<ProducerInTable> _currentProducers;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private int _numberOfConsecutiveEmptyBlocks;

        private bool _hasBeenSetup = false;

        public TransactionsHandler(ILogger<TransactionsHandler> logger, IOptions<NodeConfigurations> nodeConfigurations, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IOptions<NetworkConfigurations> networkConfigurations, IMainchainService mainchainService, IMongoDbRequesterService mongoDbRequesterService)
        {
            _networkService = networkService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations.Value;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations.Value;
            _transactionsToSend = new ThreadSafeList<TransactionSendingTrackPoco>();
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
                    if (_currentProducers.Where(p => p.Key == args.SenderAccountName).Count() != 0)
                    {
                        transactionSendingTrackPoco.ProducersAlreadyReceived.Add(args.SenderAccountName);
                    }
                }
            }
        }

        public Task Start()
        {
            _logger.LogDebug("Task starting.");
            Task = Task.Run(async () => await Execute());
            return Task;
        }

        public async Task RemoveIncludedTransactions(uint numberOfIncludedTransactions, string lastValidBlockHash)
        {
            if (numberOfIncludedTransactions == 0) _numberOfConsecutiveEmptyBlocks++;
            else _numberOfConsecutiveEmptyBlocks = 0;

            var transactionsRemovedSequenceNumbers = await _mongoDbRequesterService.RemoveAlreadyIncludedTransactionsDBAsync(_nodeConfigurations.AccountName, numberOfIncludedTransactions, lastValidBlockHash);

            foreach (var sequenceNumber in transactionsRemovedSequenceNumbers)
            {
                var transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber == sequenceNumber).SingleOrDefault();
                _transactionsToSend.Remove(transactionSendingTrackPoco);
            }

        }
        public void AddScriptTransactionToSend(Transaction transaction)
        {
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
            while (_transactionsToSend.Count() != 0)
            {
                _currentProducers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
                await TryToSendTransactions();
                await Task.Delay(WAIT_TIME_IN_SECONDS * 1000);
            }
        }

        private async Task TryToSendTransactions()
        {
            var listPeerConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().ToList();
            ListHelper.Shuffle<PeerConnection>(listPeerConnections);
            foreach (var peerConnection in listPeerConnections)
            {
                if (peerConnection.ConnectionState == ConnectionStateEnum.Connected)
                {
                    var transactionsSendingTrackPocos = _transactionsToSend.GetEnumerable().Where(p => !p.ProducersAlreadyReceived.GetEnumerable().Contains(peerConnection.ConnectionAccountName)).ToList();

                    foreach (var transactionSendingTrack in transactionsSendingTrackPocos)
                    {
                        if (transactionSendingTrack.ProducersAlreadyReceived.Count() > Math.Floor((double)(_currentProducers.Count() / 2))
                            && _currentProducers.Count > _numberOfConsecutiveEmptyBlocks)
                        {
                            transactionsSendingTrackPocos.Remove(transactionSendingTrack);
                        }
                    }
                    if (transactionsSendingTrackPocos.Count != 0)
                        await SendScriptTransactionsToProducer(transactionsSendingTrackPocos.Select(p => p.Transaction), peerConnection);
                }
            }
        }

        private async Task SendScriptTransactionsToProducer(IEnumerable<Transaction> transactions, PeerConnection peerConnection)
        {
            var data = new List<byte>();

            foreach (var transaction in transactions.Take(MAX_TRANSACTIONS_PER_MESSAGE))
            {
                var transactionBytes = transaction.ConvertToProto().ToByteArray();
                data.AddRange(BitConverter.GetBytes(transactionBytes.Count()));
                data.AddRange(transactionBytes);
            }
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransactions, data.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.PublicIpAddress + ":" + _networkConfigurations.TcpPort, _nodeConfigurations.AccountName, peerConnection.IPEndPoint);
            await _networkService.SendMessageAsync(message);
        }

        private async Task LoadTransactionsFromDatabase()
        {
            try
            {
                await _mongoDbRequesterService.CreateTransactionInfoIfNotExists(_nodeConfigurations.AccountName);
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