using System;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BlockBase.Network.IO;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using BlockBase.Network.IO.Enums;
using System.Net;
using BlockBase.Domain.Protos;
using System.Text;
using Google.Protobuf;
using System.Linq;
using BlockBase.Domain.Blockchain;
using Newtonsoft.Json;
using BlockBase.Utils.Crypto;
using System.Collections.Generic;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
using BlockBase.Domain.Database.Operations;
using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;

namespace Blockbase.ProducerD.Commands
{
    class SendTransactionsCommand : IExecutionCommand
    {
        private ProducerTestConfigurations _producerTestConfigurations;
        private ILogger _logger;
        private IServiceProvider _serviceProvider;

        public SendTransactionsCommand(ProducerTestConfigurations producerTestConfigurations, ILogger logger, IServiceProvider serviceProvider)
        {
            _producerTestConfigurations = producerTestConfigurations;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        
        public async  Task ExecuteAsync()
        {
            var clientAccountName = _producerTestConfigurations?.ClientAccountName;
            var clientPrivateKey = _producerTestConfigurations?.ClientAccountPrivateKey;
            var clientPublicKey = _producerTestConfigurations?.ClientAccountPublicKey;
            var networkService = _serviceProvider.GetService<INetworkService>();
            
            var infoDic = new Dictionary<string, int> ()
            {
                {"EOS7ZVA69r7EG4PhgeJNaQKpD32SEosZp1uLq3YAqqdJSjCWoCnQT", 40401},
                {"EOS5xw8VWkeotP9GFUFbnP4XUxjq2e5TX436CLNkLgjBKFqNaU3MF", 40402}
                // {"EOS7WTGFfB4bf2J9TjD3PKJkwoebJ4yghCavACwuHS8bU29yqexZD", 40403}
            };

            foreach(var keyValuePair in infoDic)
            {
                var destination = new IPEndPoint(IPAddress.Parse("127.0.0.1"), keyValuePair.Value);
                var messages = CreateMessages(clientPublicKey, clientPrivateKey, "127.0.0.1:40410", destination, clientAccountName);
                foreach(var message in messages) await networkService.SendMessageAsync(message);
            }
            
        }

        public List<NetworkMessage> CreateMessages(string senderPublicKey, string senderPrivateKey, string senderEndPoint, IPEndPoint destination, string clientAccountName)
        {
            var sql = new CreateDatabaseOperation(){ Database =  new Database("localhost", "myDB")};
            var json =JsonConvert.SerializeObject(sql);

            var t1 = CreateTransaction(json, 1, "myDB", MongoDbConstants.CREATE_DATABASE, senderPrivateKey);
            byte[] payload = TransactionProtoToMessageData(t1.ConvertToProto(), clientAccountName);
            var messages = new List<NetworkMessage>();

            messages.Add(new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, payload, TransportTypeEnum.Tcp, senderPrivateKey, senderPublicKey, senderEndPoint, destination));
            
            var columns = new List<Column>
            {
                new PrimaryColumn("Id"),
                new NormalColumn("Names",false, 500, 10),
                new RangeColumn("Value", false, 500, 20, 120)
            };
            var table = new Table("Users", columns);
            CreateTableOperation createTable = new CreateTableOperation { Table = table };
            json = JsonConvert.SerializeObject(createTable);
            var t2 = CreateTransaction(json, 2, "myDB", MongoDbConstants.CREATE_TABLE, senderPrivateKey);
            payload = TransactionProtoToMessageData(t2.ConvertToProto(), clientAccountName);
            messages.Add(new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, payload, TransportTypeEnum.Tcp, senderPrivateKey, senderPublicKey, senderEndPoint, destination));
            return messages;
        }

        private Transaction CreateTransaction(string json, ulong sequenceNumber, string databaseName, string operation, string senderPrivateKey)
        {
            var transaction =  new Transaction()
            { 
                Json = json, 
                BlockHash = new byte[0], 
                SequenceNumber = sequenceNumber,
                Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                TransactionHash = new byte[0],
                Signature = "",
                TransactionType = operation,
                DatabaseName = databaseName
            };

            var serializedTransaction = JsonConvert.SerializeObject(transaction);
            var transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

            transaction.TransactionHash = transactionHash;
            transaction.Signature = SignatureHelper.SignHash(senderPrivateKey, transactionHash);
            return transaction;
        }

        private byte[] TransactionProtoToMessageData(TransactionProto transactionProto, string sidechainName)
        {
            var transactionBytes = transactionProto.ToByteArray();
            // logger.LogDebug($"Block Bytes {HashHelper.ByteArrayToFormattedHexaString(blockBytes)}");

            var sidechainNameBytes = Encoding.UTF8.GetBytes(sidechainName);
            // logger.LogDebug($"Sidechain Name Bytes {HashHelper.ByteArrayToFormattedHexaString(sidechainNameBytes)}");

            short lenght = (short) sidechainNameBytes.Length;
            // logger.LogDebug($"Lenght {lenght}");

            var lengthBytes = BitConverter.GetBytes(lenght);
            // logger.LogDebug($"Lenght Bytes {HashHelper.ByteArrayToFormattedHexaString(lengthBytes)}");

            var data = lengthBytes.Concat(sidechainNameBytes).Concat(transactionBytes).ToArray();
            // logger.LogDebug($"Data {HashHelper.ByteArrayToFormattedHexaString(data)}");

            return data;
        }

        public string GetCommandHelp()
        {
            return "strx";
        }

        public bool TryParseCommand(string commandStr)
        {
            return commandStr == "strx";
        }
    }
}