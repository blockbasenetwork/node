using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
        private int TIME_BETWEEN_PRODUCERS_IN_MILLISECONDS = 2000;
        private readonly int MAX_TRANSACTIONS_PER_MESSAGE = 50;
        private ThreadSafeList<TransactionSendingTrackPoco> _transactionsToSend;
        private IMainchainService _mainchainService;
        private IList<ProducerInTable> _currentProducers;

        public TransactionSender(ILogger<TransactionSender> logger, IOptions<NodeConfigurations> nodeConfigurations, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IOptions<NetworkConfigurations> networkConfigurations, SidechainKeeper sidechainKeeper, IMainchainService mainchainService)
        {
            _networkService = networkService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations.Value;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations.Value;
            _networkService.SubscribeTransactionConfirmationReceivedEvent(MessageForwarder_TransactionConfirmationReceived);
            _transactionsToSend = new ThreadSafeList<TransactionSendingTrackPoco>();
            _mainchainService = mainchainService;
        }

        private void MessageForwarder_TransactionConfirmationReceived(MessageForwarder.TransactionConfirmationReceivedEventArgs args, IPEndPoint sender)
        {
            _logger.LogDebug($"Received confirmation of transaction {args.TransactionSequenceNumber} from {args.SenderAccountName}.");
            var transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber == args.TransactionSequenceNumber).SingleOrDefault();
            if (transactionSendingTrackPoco != null)
            {
                if (_currentProducers.Where(p => p.Key == args.SenderAccountName).Count() != 0)
                {
                    transactionSendingTrackPoco.ProducersAlreadyReceived.Add(args.SenderAccountName);
                    if (transactionSendingTrackPoco.ProducersAlreadyReceived.Count() > Math.Floor((double)(_currentProducers.Count() / 2)))
                    {
                        _transactionsToSend.Remove(transactionSendingTrackPoco);
                        _logger.LogDebug($"Removing {args.TransactionSequenceNumber}.");
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

        private async Task Execute()
        {
            while (true)
            {
                _currentProducers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
                await TryToSendTransactions();

                if (_transactionsToSend.Count() == 0)
                    return;
                else
                    await Task.Delay(1000);
            }
        }

        private async Task TryToSendTransactions()
        {
            foreach (var peerConnection in _peerConnectionsHandler.CurrentPeerConnections)
            {
                if (peerConnection.ConnectionState == ConnectionStateEnum.Connected)
                {
                    var transactionsToSendToProducer = _transactionsToSend.GetEnumerable().Where(p => !p.ProducersAlreadyReceived.GetEnumerable().Contains(peerConnection.ConnectionAccountName)).Select(p => p.Transaction);
                    // _logger.LogDebug($"Sending transaction from {transactionsPerScriptSendingTrackPoco.Transactions.First().SequenceNumber} to {transactionsPerScriptSendingTrackPoco.Transactions.Last().SequenceNumber} to {peerConnection.ConnectionAccountName}.");
                    await SendScriptTransactionsToProducer(transactionsToSendToProducer, peerConnection);
                }
                await Task.Delay(TIME_BETWEEN_PRODUCERS_IN_MILLISECONDS);
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
    }
}