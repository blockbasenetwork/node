using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Protos;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;
using static BlockBase.Network.Rounting.MessageForwarder;
using System.Collections.Generic;

namespace BlockBase.Runtime.Sidechain
{
    public class BlockSender
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;
        private string _endPoint;

        public BlockSender(ILogger<BlockSender> logger, IOptions<NodeConfigurations> nodeConfigurations, SystemConfig systemConfig, INetworkService networkService, IMongoDbProducerService mongoDbProducerService)
        {
            _networkService = networkService;
            _logger = logger;
            _networkService.SubscribeBlocksRequestReceivedEvent(MessageForwarder_BlockRequest);
            _mongoDbProducerService = mongoDbProducerService;

            _nodeConfigurations = nodeConfigurations?.Value;
            _endPoint = systemConfig.IPAddress + ":" + systemConfig.TcpPort;
        }

        private async void MessageForwarder_BlockRequest(BlocksRequestReceivedEventArgs args)
        {
            _logger.LogDebug("Block request received.");
            var blocksToSend = new List<Block>();
            foreach(var sequenceNumber in args.BlocksSequenceNumber)
            {
                var block = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(args.ClientAccountName, (ulong) sequenceNumber, (ulong) sequenceNumber)).SingleOrDefault();
                if(block == null) 
                {
                    _logger.LogWarning("No block with sequence number "+ sequenceNumber + " to send.");
                    return;
                }              
                blocksToSend.Add(block);
            } 

            foreach(Block block in blocksToSend)
            {
                _logger.LogDebug("Going to send block: " + block.BlockHeader.SequenceNumber);
                var data = BlockProtoToMessageData(block.ConvertToProto(), args.ClientAccountName);
                var message = new NetworkMessage(NetworkMessageTypeEnum.SendBlock, data, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, args.Sender);
                await _networkService.SendMessageAsync(message);
            }
        }
        
        internal async Task SendBlockToSidechainMembers(SidechainPool sidechainPool, BlockProto blockProto, string endPoint)
        {
            
            var data = BlockProtoToMessageData(blockProto, sidechainPool.ClientAccountName);
            var connectedProducersInSidechain = sidechainPool.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection != null && m.PeerConnection.ConnectionState == ConnectionStateEnum.Connected);
            foreach(ProducerInPool producerConnected in connectedProducersInSidechain)
            {
                //_logger.LogDebug($"Block sent to {producerConnected.ProducerInfo.AccountName}. Signed by: {blockProto.BlockHeader.Producer}");

                var message = new NetworkMessage(NetworkMessageTypeEnum.SendMinedBlock, data, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, endPoint, _nodeConfigurations.AccountName, producerConnected.PeerConnection.IPEndPoint);
                await _networkService.SendMessageAsync(message);
            }
        }

        private byte[] BlockProtoToMessageData(BlockProto blockProto, string sidechainName)
        {
            var blockBytes = blockProto.ToByteArray();
            // logger.LogDebug($"Block Bytes {HashHelper.ByteArrayToFormattedHexaString(blockBytes)}");

            var sidechainNameBytes = Encoding.UTF8.GetBytes(sidechainName);
            // logger.LogDebug($"Sidechain Name Bytes {HashHelper.ByteArrayToFormattedHexaString(sidechainNameBytes)}");

            short lenght = (short) sidechainNameBytes.Length;
            // logger.LogDebug($"Lenght {lenght}");

            var lengthBytes = BitConverter.GetBytes(lenght);
            // logger.LogDebug($"Lenght Bytes {HashHelper.ByteArrayToFormattedHexaString(lengthBytes)}");

            var data = lengthBytes.Concat(sidechainNameBytes).Concat(blockBytes).ToArray();
            // logger.LogDebug($"Data {HashHelper.ByteArrayToFormattedHexaString(data)}");

            return data;
        }
    }
}