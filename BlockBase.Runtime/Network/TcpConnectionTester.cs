using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.P2P;
using Open.P2P.Listeners;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.Rounting.MessageForwarder;

namespace BlockBase.Runtime.Network
{
#pragma warning disable
    public class TcpConnectionTester
    {
        private INetworkService _networkService;

        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private ThreadSafeList<IPEndPoint> _connections;
        private ILogger<TcpConnector> _logger;
        private SystemConfig _systemConfig;

        private DateTime _lastUsage;

        public TcpConnectionTester(SystemConfig systemConfig, ILogger<TcpConnector> logger, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IOptions<NodeConfigurations> nodeConfigurations)
        {
            _logger = logger;
            // _tcpConnector = tcpConnector;
            _networkService = networkService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkService.SubscribePingReceivedEvent(MessageForwarder_PingMessageReceived);
            _nodeConfigurations = nodeConfigurations.Value;
            _connections = new ThreadSafeList<IPEndPoint>();
            _systemConfig = systemConfig;
            _lastUsage = DateTime.UtcNow;

        }
        public async Task<Peer> TestListen(IPEndPoint remoteEndPoint)
        {
            _lastUsage = DateTime.UtcNow;

            if (_connections.Contains((ip) => ip.Address.ToString() == remoteEndPoint.Address.ToString())) return null;

            var peerConnection = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Where(p => p.IPEndPoint.Address.ToString() == remoteEndPoint.Address.ToString() && p.IPEndPoint.Port == remoteEndPoint.Port).SingleOrDefault();

            _networkService.SubscribePeerConnectedEvent(TcpConnector_PeerConnected);
            _networkService.SubscribePongReceivedEvent(MessageForwarder_PongMessageReceived);

            if (peerConnection != null)
            {
                _logger.LogInformation($"Using existing peer connection to {peerConnection.Peer.EndPoint.Address.ToString()}:{peerConnection.Peer.EndPoint.Port}");
                PrepareAndSendData(peerConnection.Peer);
                return peerConnection.Peer;
            }

            _logger.LogInformation($"Trying to connect to {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}");
            var peer = await _networkService.ConnectAsync(remoteEndPoint);//, new IPEndPoint(_systemConfig.IPAddress, _systemConfig.TcpPort));
            return peer;
        }

        private void TcpConnector_PeerConnected(object sender, PeerConnectedEventArgs args)
        {
            PrepareAndSendData(args.Peer);
        }

        private void PrepareAndSendData(Peer peer)
        {
            _logger.LogInformation($"Successfully tested connection to {peer.EndPoint.Address.ToString()}:{peer.EndPoint.Port}");

            _connections.Add(peer.EndPoint, (ip) => ip.Address.ToString() == peer.EndPoint.Address.ToString());

            DisconnectAfter(peer, TimeSpan.FromSeconds(20));
            SendDataUntilPeerExists(1, TimeSpan.FromSeconds(1), peer.EndPoint);

        }

        private async Task SendDataUntilPeerExists(int number, TimeSpan delayBetweenMessages, IPEndPoint ipEndPoint)
        {

            try
            {
                if (!_connections.Contains((ip) => ip.Address.ToString() == ipEndPoint.Address.ToString())) return;

                var bytes = Encoding.Unicode.GetBytes($"Hello message #{number} from {_systemConfig.IPAddress}");
                byte[] payload = BitConverter.GetBytes(number);
                var type = NetworkMessageTypeEnum.Ping;
                string endPoint = _systemConfig.IPAddress + ":" + _systemConfig.TcpPort;
                var message = new NetworkMessage(type, payload, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, endPoint, _nodeConfigurations.AccountName, ipEndPoint);
                await _networkService.SendMessageAsync(message);
                _logger.LogInformation($"Sent ping message #{number}");
                await Task.Delay(delayBetweenMessages);
                SendDataUntilPeerExists(number + 1, delayBetweenMessages, ipEndPoint);
            }
            catch
            {
                _logger.LogInformation("Failed to send message");
            }
        }

        private void MessageForwarder_PongMessageReceived(PongReceivedEventArgs args, IPEndPoint sender)
        {
            _logger.LogInformation($"Received pong message from {sender.Address.ToString()} with number: {args.nonce}");
        }

        private void MessageForwarder_PingMessageReceived(PingReceivedEventArgs args, IPEndPoint sender)
        {
            //check if the nounce is between 0 and 20 so it doesn't print other ping messages used to maintain connections open
            if(args.nonce > 0 && args.nonce <= 20)
                _logger.LogInformation($"Received ping message from {sender.Address.ToString()}:{sender.Port} with number: {args.nonce}");
        }


        private async Task DisconnectAfter(Peer peer, TimeSpan timeSpan)
        {
            try
            {
                await Task.Delay(timeSpan);

                _networkService.UnSubscribePeerConnectedEvent(TcpConnector_PeerConnected);
                _networkService.UnSubscribePongReceivedEvent(MessageForwarder_PongMessageReceived);

                _connections.Remove(peer.EndPoint);
                await Task.Delay(TimeSpan.FromSeconds(2));

                //if the peer connections handler knows this peer don't force a disconnect
                if(!_peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Any(p => p.IPEndPoint.Address.ToString() == peer.EndPoint.Address.ToString() && p.IPEndPoint.Port == peer.EndPoint.Port))
                    _networkService.DisconnectPeer(peer);

                _logger.LogDebug($"Peer {peer.EndPoint.Address.ToString()}:{peer.EndPoint.Port} disconnected automatically after {timeSpan.TotalSeconds} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error disconnecting peer {peer.EndPoint.Address.ToString()}:{peer.EndPoint.Port} automatically: {ex.Message}");
            }
        }
    }
}