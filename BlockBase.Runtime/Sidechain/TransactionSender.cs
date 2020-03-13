using System;
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
using BlockBase.Network.Rounting;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Threading;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Sidechain
{
    public class TransactionSender
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        public TaskContainer TaskContainer { get; private set; }
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NetworkConfigurations _networkConfigurations;
        private int WAIT_FOR_RESPONSE_TIME_IN_SECONDs = 120;
        private int TIME_BETWEEN_SENDING_TRANSACTIONS_IN_MILLISECONDS = 2000;
        private ThreadSafeList<TransactionSendingTrackPoco> _transactionsToSend;
        public ThreadSafeList<Transaction> TransactionWaitList;

        public TransactionSender(ILogger logger, NodeConfigurations nodeConfigurations, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, NetworkConfigurations networkConfigurations)
        {
            _networkService = networkService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations;
            _networkService.SubscribeTransactionConfirmationReceivedEvent(MessageForwarder_TransactionConfirmationReceived);
            _transactionsToSend = new ThreadSafeList<TransactionSendingTrackPoco>();
            TransactionWaitList = new ThreadSafeList<Transaction>();
        }

        private void MessageForwarder_TransactionConfirmationReceived(MessageForwarder.TransactionConfirmationReceivedEventArgs args, IPEndPoint sender)
        {
            _logger.LogDebug($"Received confirmation of transaction {args.TransactionSequenceNumber} from {args.SenderAccountName}.");
            var transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.SequenceNumber == args.TransactionSequenceNumber).SingleOrDefault();
            if (transactionSendingTrackPoco != null)
            {
                var peerConnection = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Where(p => p.ConnectionAccountName == args.SenderAccountName).SingleOrDefault();
                transactionSendingTrackPoco.ProducersToSend.Remove(peerConnection);

                if (transactionSendingTrackPoco.ProducersToSend.Count() < Math.Ceiling((double)_peerConnectionsHandler.CurrentPeerConnections.Count() / 2))
                    _transactionsToSend.Remove(transactionSendingTrackPoco);
            }
        }

        public TaskContainer Start()
        {
            TaskContainer = TaskContainer.Create(async () => await Execute());
            TaskContainer.Start();
            return TaskContainer;
        }

        private async Task Execute()
        {
            while (_transactionsToSend.Count() != 0 || TransactionWaitList.Count() != 0)
            {
                await Task.Delay(1000);
                TryToRemoveTransactionsFromWaitingList();
                await TryToSendTransactionsAgain();
            }
        }

        private async Task TryToSendTransactionsAgain()
        {
            foreach (var transactionSendingTrackPoco in _transactionsToSend)
            {
                if (transactionSendingTrackPoco.NextTimeToSendTransaction < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    foreach (var peerConnection in transactionSendingTrackPoco.ProducersToSend)
                    {
                        if (peerConnection.ConnectionState == ConnectionStateEnum.Connected)
                            await SendTransactionToProducer(transactionSendingTrackPoco.Transaction, peerConnection);

                        transactionSendingTrackPoco.NextTimeToSendTransaction = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (WAIT_FOR_RESPONSE_TIME_IN_SECONDs * 1000);
                        await Task.Delay(TIME_BETWEEN_SENDING_TRANSACTIONS_IN_MILLISECONDS);
                    }
                }
            }
        }
        private void TryToRemoveTransactionsFromWaitingList()
        {
            if(_peerConnectionsHandler.CurrentPeerConnections.Count() != 0)
            {
                foreach(var transaction in TransactionWaitList.GetEnumerable())
                {
                    foreach(var peerConnection in _peerConnectionsHandler.CurrentPeerConnections)
                    {
                        AddWaitingTransactionToProducer(peerConnection, transaction);
                    }
                    TransactionWaitList.Remove(transaction);
                }
            }  
        }
        private byte[] TransactionProtoToMessageData(TransactionProto transactionProto, string sidechainName)
        {
            var transactionBytes = transactionProto.ToByteArray();
            // logger.LogDebug($"Block Bytes {HashHelper.ByteArrayToFormattedHexaString(blockBytes)}");

            var sidechainNameBytes = Encoding.UTF8.GetBytes(sidechainName);
            // logger.LogDebug($"Sidechain Name Bytes {HashHelper.ByteArrayToFormattedHexaString(sidechainNameBytes)}");

            short lenght = (short)sidechainNameBytes.Length;
            // logger.LogDebug($"Lenght {lenght}");

            var lengthBytes = BitConverter.GetBytes(lenght);
            // logger.LogDebug($"Lenght Bytes {HashHelper.ByteArrayToFormattedHexaString(lengthBytes)}");

            var data = lengthBytes.Concat(sidechainNameBytes).Concat(transactionBytes).ToArray();
            // logger.LogDebug($"Data {HashHelper.ByteArrayToFormattedHexaString(data)}");

            return data;
        }
        public void AddWaitingTransactionToProducer(PeerConnection peerConnection, Transaction transaction)
        {
            TransactionSendingTrackPoco transactionSendingTrackPoco;

            if (_transactionsToSend.GetEnumerable().Where(t => t.Transaction.TransactionHash.SequenceEqual(transaction.TransactionHash)).Count() == 0)
            {
                transactionSendingTrackPoco = new TransactionSendingTrackPoco()
                {
                    Transaction = transaction,
                    ProducersToSend = new ThreadSafeList<PeerConnection>(),
                    NextTimeToSendTransaction = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                _transactionsToSend.Add(transactionSendingTrackPoco);
            }
            else
                transactionSendingTrackPoco = _transactionsToSend.GetEnumerable().Where(t => t.Transaction.TransactionHash.SequenceEqual(transaction.TransactionHash)).SingleOrDefault();

            transactionSendingTrackPoco.ProducersToSend.Add(peerConnection);
        }
        private async Task SendTransactionToProducer(Transaction transaction, PeerConnection peerConnection)
        {
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, TransactionProtoToMessageData(transaction.ConvertToProto(), _nodeConfigurations.AccountName), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.LocalIpAddress + ":" + _networkConfigurations.LocalTcpPort, peerConnection.ConnectionAccountName, peerConnection.IPEndPoint);
            await _networkService.SendMessageAsync(message);
        }
    }
}