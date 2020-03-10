using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Protos;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;

namespace BlockBase.Runtime.Sidechain
{
    public class TransactionSender
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        private string _localEndPoint;
        public TaskContainer TaskContainer { get; private set; }
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NetworkConfigurations _networkConfigurations;

        public TransactionSender(ILogger logger, NodeConfigurations nodeConfigurations, INetworkService networkService,  PeerConnectionsHandler peerConnectionsHandler, NetworkConfigurations networkConfigurations)
        {
            _networkService = networkService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations;
        }

        public TaskContainer Start(string queryToExecute, ulong transactionNumber, string databaseName)
        {
            TaskContainer = TaskContainer.Create(async () => await Execute(queryToExecute, transactionNumber, databaseName));
            TaskContainer.Start();
            return TaskContainer;
        }

        private async Task Execute(string queryToExecute, ulong transactionNumber, string databaseName)
        {
             foreach (var peerConnection in _peerConnectionsHandler.CurrentPeerConnections)
            {
                var transaction = CreateTransaction(queryToExecute, transactionNumber, databaseName, _nodeConfigurations.ActivePrivateKey);
                var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, TransactionProtoToMessageData(transaction.ConvertToProto(), _nodeConfigurations.AccountName), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.LocalIpAddress + ":" + _networkConfigurations.LocalTcpPort, peerConnection.ConnectionAccountName, peerConnection.IPEndPoint);
                await _networkService.SendMessageAsync(message);
            }
        }


         private Transaction CreateTransaction(string json, ulong sequenceNumber, string databaseName, string senderPrivateKey)
        {
            var transaction = new Transaction()
            {
                Json = json,
                BlockHash = new byte[0],
                SequenceNumber = sequenceNumber,
                Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                TransactionHash = new byte[0],
                Signature = "",
                DatabaseName = databaseName
            };

            var serializedTransaction = JsonConvert.SerializeObject(transaction);
            var transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

            transaction.TransactionHash = transactionHash;
            transaction.Signature = SignatureHelper.SignHash(senderPrivateKey, transactionHash);
            _logger.LogDebug(transaction.BlockHash.ToString() + ":" + transaction.DatabaseName + ":" + transaction.SequenceNumber + ":" + transaction.Json + ":" + transaction.Signature + ":" + transaction.Timestamp);
            return transaction;
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
    }
}