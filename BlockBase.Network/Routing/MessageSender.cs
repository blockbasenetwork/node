using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BlockBase.Network.Rounting
{
    public class MessageSender
    {
        private TcpConnector TcpConnector { get; set; }
        private UdpConnector UdpConnector { get; set; }

        private readonly ILogger _logger;

        public MessageSender(TcpConnector tcpConnector, UdpConnector udpConnector, ILogger<MessageSender> logger)
        {
            TcpConnector = tcpConnector;
            UdpConnector = udpConnector;
            _logger = logger;
        }

        public async Task SendMessage(NetworkMessage message)
        {
            try
            {
                //TODO: Change to verifiction "if message.Destination != null" once it's been verified it won't cause problems
                if (message.TransportType == TransportTypeEnum.Tcp)
                {
                    var messagePacket = message.ConvertToPacket();

                    //Code to help debug
                    var payloadHash = HashHelper.ByteArrayToFormattedHexaString(HashHelper.Sha256Data(message.Payload));
                    //_logger.LogDebug($"MESSAGE SENT | Hash: {payloadHash} | Destination: {message.Destination} | Type: {message.NetworkMessageType}");

                    await TcpConnector.SendData(messagePacket, message.Destination);
                }
                else
                {
                    _logger.LogError("Request to forward outgoing message without transport defined");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to send message to: {message.Destination.Address}:{message.Destination.Port}");
                _logger.LogDebug(ex, "MessageSender-SendMessage crashed: " + ex.Message);
            }
        }
    }
}