using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Analysis;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using System;

namespace BlockBase.Network.Rounting
{
    public class MessageReceiver
    {
        private MessageForwarder MessageForwarder { get; set; }
        private TcpConnector TcpConnector { get; set; }
        private UdpConnector UdpConnector { get; set; }

        private readonly ILogger _logger;

        public MessageReceiver(TcpConnector tcpConnector, UdpConnector udpConnector, ILogger<MessageReceiver> logger, MessageForwarder messageForwarder)
        {
            MessageForwarder = messageForwarder;
            TcpConnector = tcpConnector;
            UdpConnector = udpConnector;
            _logger = logger;

            TcpConnector.DataReceived += TcpConnector_DataReceived;
            UdpConnector.DataReceived += UdpConnector_DataReceived;
        }

        private void ProcessIncommingMessage(RawNetworkMessage rawNetworkMessage)
        {
            try
            {
                var messageParser = new MessageParser();
                var parsingResult = messageParser.AnalyseAndParseMessage(rawNetworkMessage, out NetworkMessage networkMessage);

                //TODO: rpinto - call the message analyser

                if (parsingResult == MessageParsingResultEnum.Success)
                {
                    //Code to help debug
                    var payloadHash = HashHelper.ByteArrayToFormattedHexaString(HashHelper.Sha256Data(networkMessage.Payload));
                    _logger.LogDebug($"MESSAGE RECEIVED | Hash: {payloadHash} | Sender: {networkMessage.Sender} | Type: {networkMessage.NetworkMessageType}");

                    MessageForwarder.ProcessReceivedMessage(networkMessage);
                }
                else
                {
                    _logger.LogDebug($"Error analysing and parsing message.");
                    //EventManager.Instance.RegisterEvent(this, EventSeverityLevelEnum.Low, "Discarded Message :" + message.ToString());
                }
                MessageForwarder.ReleaseAllExpiredMessageEvents();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Receiving message crashed: " + ex.Message);
            }
        }

        private void TcpConnector_DataReceived(object sender, DataReceivedEventArgs args)
        {
            ProcessIncommingMessage(new RawNetworkMessage { IPEndPoint = args.Peer.EndPoint, Bytes = args.Data });
        }

        private void UdpConnector_DataReceived(object sender, Open.P2P.EventArgs.UdpPacketReceivedEventArgs args)
        {
            ProcessIncommingMessage(new RawNetworkMessage { IPEndPoint = args.EndPoint, Bytes = args.Data });
        }
    }
}