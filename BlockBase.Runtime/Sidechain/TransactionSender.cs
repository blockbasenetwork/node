using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Protos;
using BlockBase.Network;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Rounting;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Sidechain
{
    public class TransactionSender
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        public Task Task { get; private set; }
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NetworkConfigurations _networkConfigurations;
        private readonly int MAX_TRANSACTIONS_PER_MESSAGE = 50;
        private ThreadSafeList<TransactionSendingTrackPoco> _transactionsToSend;
        private IMainchainService _mainchainService;
        private IList<ProducerInTable> _currentProducers;
        private IMongoDbProducerService _mongoDbProducerService;
        private int _numberOfConfirmationsAwaiting;
        private int _numberOfConfirmationsToWait;
        private long _nextTimeToTry;

        public TransactionSender(ILogger<TransactionSender> logger, IOptions<NodeConfigurations> nodeConfigurations, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IOptions<NetworkConfigurations> networkConfigurations, SidechainKeeper sidechainKeeper, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService)
        {
            _networkService = networkService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations.Value;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations.Value;
            _transactionsToSend = new ThreadSafeList<TransactionSendingTrackPoco>();
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _networkService.SubscribeTransactionConfirmationReceivedEvent(MessageForwarder_TransactionConfirmationReceived);
            LoadTransactionsFromDatabase().Wait();
        }

        private void MessageForwarder_TransactionConfirmationReceived(MessageForwarder.TransactionConfirmationReceivedEventArgs args, IPEndPoint sender)
        {
            foreach (var sequenceNumber in args.TransactionSequenceNumbers)
            {
                // _logger.LogDebug($"Received confirmation of transaction {sequenceNumber} from {args.SenderAccountName}.");
                var transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber == sequenceNumber).SingleOrDefault();
                if (transactionSendingTrackPoco != null)
                {
                    if (_currentProducers.Where(p => p.Key == args.SenderAccountName).Count() != 0)
                    {
                        transactionSendingTrackPoco.ProducersAlreadyReceived.Add(args.SenderAccountName);
                        if (transactionSendingTrackPoco.ProducersAlreadyReceived.Count() > Math.Floor((double)(_currentProducers.Count() / 2)))
                        {
                            _transactionsToSend.Remove(transactionSendingTrackPoco);
                        }
                    }
                }
            }
            _mongoDbProducerService.RemoveAlreadySentTransactionsDBAsync(
                _nodeConfigurations.AccountName,
                _transactionsToSend.GetEnumerable().Select(t => t.Transaction.SequenceNumber)
                );
            _numberOfConfirmationsAwaiting--;
        }

        public Task Start()
        {
            _logger.LogDebug("Task starting.");
            Task = Task.Run(async () => await Execute());
            return Task;
        }

        private async Task Execute()
        {
            while (true)
            {
                _numberOfConfirmationsAwaiting = 0;
                _numberOfConfirmationsToWait = 0;
                _currentProducers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
                await TryToSendTransactions();

                if (_transactionsToSend.Count() == 0)
                {
                    return;
                }
                else
                {
                    while (_nextTimeToTry > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && _numberOfConfirmationsAwaiting > Math.Floor((double)(_numberOfConfirmationsToWait / 2)))
                    {
                        await Task.Delay(50);
                    }
                }
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
                    var transactionsToSendToProducer = _transactionsToSend.GetEnumerable().Where(p => !p.ProducersAlreadyReceived.GetEnumerable().Contains(peerConnection.ConnectionAccountName)).Select(p => p.Transaction);
                    // _logger.LogDebug($"Sending transaction from {transactionsPerScriptSendingTrackPoco.Transactions.First().SequenceNumber} to {transactionsPerScriptSendingTrackPoco.Transactions.Last().SequenceNumber} to {peerConnection.ConnectionAccountName}.");
                    await SendScriptTransactionsToProducer(transactionsToSendToProducer, peerConnection);
                    _numberOfConfirmationsAwaiting++;
                }
            }
            _numberOfConfirmationsToWait = _numberOfConfirmationsAwaiting;
            _nextTimeToTry = DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds();
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

        private async Task SendScriptTransactionsToProducer(IEnumerable<Transaction> transactions, PeerConnection peerConnection)
        {
            var data = new List<byte>();

            foreach (var transaction in transactions.Take(MAX_TRANSACTIONS_PER_MESSAGE))
            {
                var transactionBytes = transaction.ConvertToProto().ToByteArray();
                data.AddRange(BitConverter.GetBytes(transactionBytes.Count()));
                data.AddRange(transactionBytes);
            }
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, data.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.LocalIpAddress + ":" + _networkConfigurations.LocalTcpPort, _nodeConfigurations.AccountName, peerConnection.IPEndPoint);
            await _networkService.SendMessageAsync(message);
        }

        private async Task LoadTransactionsFromDatabase()
        {
            var transactions = await _mongoDbProducerService.RetrieveLastLooseTransactions(_nodeConfigurations.AccountName);
            foreach (var transaction in transactions)
                AddScriptTransactionToSend(transaction);
        }
    }
}