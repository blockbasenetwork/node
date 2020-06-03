using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Protos;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Utils;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;
using static BlockBase.Network.Rounting.MessageForwarder;
using System.Collections.Generic;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.Helpers;

namespace BlockBase.Runtime.Network
{
    public class BlockRequestsHandler
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;
        private string _localEndPoint;
        private IMainchainService _mainchainService;
        private HistoryValidation _historyValidation;

        public BlockRequestsHandler(ILogger<BlockRequestsHandler> logger, IOptions<NodeConfigurations> nodeConfigurations, SystemConfig systemConfig, INetworkService networkService, IMongoDbProducerService mongoDbProducerService, IMainchainService mainchainService)
        {
            _networkService = networkService;
            _logger = logger;
            _networkService.SubscribeBlocksRequestReceivedEvent(MessageForwarder_BlockRequest);
            _mongoDbProducerService = mongoDbProducerService;
            _mainchainService = mainchainService;

            _nodeConfigurations = nodeConfigurations?.Value;
            _localEndPoint = systemConfig.IPAddress + ":" + systemConfig.TcpPort;
            _historyValidation = new HistoryValidation(_logger, _mongoDbProducerService, _mainchainService);
        }

        private async void MessageForwarder_BlockRequest(BlocksRequestReceivedEventArgs args)
        {
            try
            {
                _logger.LogDebug("Block request received.");
                var blocksToSend = new List<Block>();
                var data = new List<byte>();

                var historyTable = await _mainchainService.RetrieveHistoryValidationTable(args.ClientAccountName);
                if (historyTable != null)
                {
                    var blockToValidateSequenceNumber = await _historyValidation.GetChosenBlockSequenceNumber(historyTable.BlockHash, args.ClientAccountName);
                    if (blockToValidateSequenceNumber.HasValue && args.BlocksSequenceNumber.Contains(blockToValidateSequenceNumber.Value))
                        args.BlocksSequenceNumber.Remove(blockToValidateSequenceNumber.Value);
                }


                foreach (var sequenceNumber in args.BlocksSequenceNumber)
                {
                    var block = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(args.ClientAccountName, sequenceNumber, sequenceNumber)).SingleOrDefault();
                    if (block == null)
                    {
                        _logger.LogWarning($"No block with sequence number {sequenceNumber} from chain {args.ClientAccountName} to send.");
                        return;
                    }
                    blocksToSend.Add(block);
                }

                var sidechainNameBytes = Encoding.UTF8.GetBytes(args.ClientAccountName);
                short sidechainNamelenght = (short)sidechainNameBytes.Length;
                var sidechainNameLengthBytes = BitConverter.GetBytes(sidechainNamelenght);
                data.AddRange(sidechainNameLengthBytes);
                data.AddRange(sidechainNameBytes);

                foreach (Block block in blocksToSend)
                {
                    var blockBytes = block.ConvertToProto().ToByteArray(); ;
                    data.AddRange(BitConverter.GetBytes(blockBytes.Count()));
                    data.AddRange(blockBytes);
                }

                var message = new NetworkMessage(NetworkMessageTypeEnum.SendBlock, data.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _localEndPoint, _nodeConfigurations.AccountName, args.Sender);
                await _networkService.SendMessageAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Block request handler crashed with exception: {e}");
            }
        }

        internal async Task SendBlockToSidechainMembers(SidechainPool sidechainPool, BlockProto blockProto, string endPoint)
        {

            var data = BlockProtoToMessageData(blockProto, sidechainPool.ClientAccountName);
            var connectedProducersInSidechain = sidechainPool.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection != null && m.PeerConnection.ConnectionState == ConnectionStateEnum.Connected);
            foreach (ProducerInPool producerConnected in connectedProducersInSidechain)
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

            short lenght = (short)sidechainNameBytes.Length;
            // logger.LogDebug($"Lenght {lenght}");

            var lengthBytes = BitConverter.GetBytes(lenght);
            // logger.LogDebug($"Lenght Bytes {HashHelper.ByteArrayToFormattedHexaString(lengthBytes)}");

            var data = lengthBytes.Concat(sidechainNameBytes).Concat(blockBytes).ToArray();
            // logger.LogDebug($"Data {HashHelper.ByteArrayToFormattedHexaString(data)}");

            return data;
        }
    }
}