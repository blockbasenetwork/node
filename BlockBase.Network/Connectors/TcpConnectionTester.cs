using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Network.IO;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Open.P2P;
using Open.P2P.Listeners;

namespace BlockBase.Network.Connectors
{
    public class TcpConnectionTester
    {
        private TcpConnector _tcpConnector;

        private ConcurrentBag<IPEndPoint> _connections;
        private ILogger<TcpConnector> _logger;
        private SystemConfig _systemConfig;

        private DateTime _lastUsage;

        public TcpConnectionTester(SystemConfig systemConfig, ILogger<TcpConnector> logger)
        {
            _logger = logger;
            _tcpConnector = new TcpConnector(systemConfig, logger);
            _tcpConnector.PeerConnected += TcpConnector_PeerConnected;
            _tcpConnector.DataReceived += TcpConnector_DataReceived;
            _connections = new ConcurrentBag<IPEndPoint>();
            _systemConfig = systemConfig;
            _lastUsage = DateTime.UtcNow;

        }
        public async Task<Peer> TestListen(IPEndPoint remoteEndPoint)
        {
            _lastUsage = DateTime.UtcNow;
            if(_tcpConnector.GetListenerStatus() == ListenerStatus.Unknown)
            {
                _tcpConnector.Start();
                ShutDownAfter(TimeSpan.FromMinutes(5));

            }
            var peer = await _tcpConnector.ConnectAsync(remoteEndPoint);//, new IPEndPoint(_systemConfig.IPAddress, _systemConfig.TcpPort));

            return peer;
        }

        private async void TcpConnector_PeerConnected(object sender, PeerConnectedEventArgs args)
        {
            _logger.LogDebug($"Successfully tested connection to {args.Peer.EndPoint.Address.ToString()}:{args.Peer.EndPoint.Port}");
            DisconnectAfter(args.Peer, TimeSpan.FromSeconds(20));
            SendDataUntilItFails(1, TimeSpan.FromSeconds(1), args.Peer.EndPoint);
        }

        private async Task SendDataUntilItFails(int number, TimeSpan delayBetweenMessages, IPEndPoint ipEndPoint) 
        {

            try
            {
                var bytes = Encoding.Unicode.GetBytes($"Hello message #{number} from {_systemConfig.IPAddress}");
                await _tcpConnector.SendData(bytes, ipEndPoint);
                _logger.LogDebug($"Sent hello message #{number}");
                await Task.Delay(delayBetweenMessages);
                SendDataUntilItFails(number+1, delayBetweenMessages, ipEndPoint);
            }
            catch 
            {
                _logger.LogDebug("Failed to send message");
            }
        }

        private async void TcpConnector_DataReceived(object sender, DataReceivedEventArgs args) 
        {
            var message = Encoding.Unicode.GetString(args.Data);
            _logger.LogDebug($"Received message from {args.Peer.EndPoint.Address.ToString()} saying: {message}");
        }

        

        private async Task DisconnectAfter(Peer peer, TimeSpan timeSpan)
        {
            try
            {
                await Task.Delay(timeSpan);
                _tcpConnector.Disconnect(peer);
                _logger.LogDebug($"Peer {peer.EndPoint.Address.ToString()}:{peer.EndPoint.Port} disconnected automatically after {timeSpan.TotalSeconds} seconds");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error disconnecting peer {peer.EndPoint.Address.ToString()}:{peer.EndPoint.Port} automatically: {ex.Message}");
            }
        }

        private async Task ShutDownAfter(TimeSpan timeSpan) 
        {
            var timeDistance = DateTime.UtcNow - _lastUsage;
            if(timeDistance > timeSpan)
            {
                _tcpConnector.Stop();
            }
            else
            {
                await Task.Delay(timeDistance);
                ShutDownAfter(timeSpan);
            }
        }
    }

    public class ConnectionInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public Task<Peer> Connection { get; set; }


    }

}