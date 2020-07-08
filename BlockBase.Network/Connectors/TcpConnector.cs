using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Open.P2P;
using Open.P2P.EventArgs;
using Open.P2P.IO;
using Open.P2P.Listeners;
using Open.P2P.Streams.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BlockBase.Network.Connectors
{
    public class TcpConnector
    {
        private CommunicationManager _connectionsManager;
        private TcpListener _listener;
        private readonly SystemConfig _systemConfig;
        private readonly ILogger _logger;

        public TcpConnector(SystemConfig systemConfig, ILogger<TcpConnector> logger)
        {
            _systemConfig = systemConfig;
            _logger = logger;
        }

        public void Start()
        {
            _listener = new TcpListener(_systemConfig.TcpPort);
            _connectionsManager = new CommunicationManager(_listener, _logger);
            _connectionsManager.ConnectionClosed += OnPeerDisconnected;
            _connectionsManager.PeerConnected += OnPeerConnected;

            _listener.Start();
        }

        public void Stop()
        {
            _connectionsManager.ConnectionClosed -= OnPeerDisconnected;
            _connectionsManager.PeerConnected -= OnPeerConnected;
            _listener.Stop();
        }

        public ListenerStatus GetListenerStatus()
        {
            return _listener != null ? _listener.Status : ListenerStatus.Unknown;
        }

        public async Task<Peer> ConnectAsync(IPEndPoint remoteEndPoint, EndPoint localEndPoint = null)
        {
            //TODO - rpinto
            //try to cast down IPV6 to IPV4...
            if (remoteEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                remoteEndPoint.Address = remoteEndPoint.Address.MapToIPv4();
            return await _connectionsManager.ConnectAsync(remoteEndPoint, localEndPoint);
        }

        public void Disconnect(Peer peer)
        {
            _connectionsManager.DisconnectPeer(peer);
        }

        public void DisconnectAllPeers()
        {
            _connectionsManager.DisconnectAllPeers();
        }

        public bool IsConnectedTo(IPEndPoint endpoint)
        {
            return _connectionsManager.IsConnectedTo(endpoint);
        }

        public async Task SendData(byte[] data, IPEndPoint endpoint)
        {
            //TODO - rpinto
            //try to cast down IPV6 to IPV4...
            if (endpoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                endpoint.Address = endpoint.Address.MapToIPv4();

            var buf = PascalStreamReader.FormatMessage(data);
            await _connectionsManager.SendAsync(buf, 0, buf.Length, new List<IPEndPoint>() { endpoint });
        }

        public async Task SendData(byte[] data, IEnumerable<IPEndPoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    endpoint.Address = endpoint.Address.MapToIPv4();
            }

            var buf = PascalStreamReader.FormatMessage(data);
            await _connectionsManager.SendAsync(buf, 0, buf.Length, endpoints);
        }

        private void OnPeerDisconnected(object sender, ConnectionEventArgs e)
        {
            _logger.LogDebug("TCP Connector :: Peer disconnected.");
            PeerDisconnected?.Invoke(this, new PeerDisconnectedEventArgs { IPEndPoint = e.EndPoint });
        }

        /// <summary>
        /// When a tcp connection is established, we will listen to his communications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnPeerConnected(object sender, PeerEventArgs e)
        {
            _logger.LogDebug("Peer connected");
            var sr = new PascalStreamReader(e.Peer.Stream);

            PeerConnected?.Invoke(this, new PeerConnectedEventArgs { Peer = e.Peer });

            byte[] bytes;
            try
            {
                while (true)
                {
                    //always read bytes sent from peer.
                    bytes = await sr.ReadBytesAsync();

                    DataReceived?.Invoke(this, new DataReceivedEventArgs { Peer = e.Peer, Data = bytes });
                }
            }
            catch(EndOfStreamException exception)
            {
                _logger.LogDebug($"Peer disconnected: {e?.Peer?.EndPoint?.Address} error: {exception.Message}");
                Disconnect(e.Peer);
            }
            catch (Exception exception)
            {
                _logger.LogDebug($"Peer communication crashed: {e?.Peer?.EndPoint?.Address} error: {exception.Message}");
            }
        }

        public event PeerConnectedEventHandler PeerConnected;

        public event PeerDisconnectedEventHandler PeerDisconnected;

        public event DataReceivedEventHandler DataReceived;

        public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs args);

        public delegate void PeerConnectedEventHandler(object sender, PeerConnectedEventArgs args);

        public delegate void PeerDisconnectedEventHandler(object sender, PeerDisconnectedEventArgs args);
    }

    public class DataReceivedEventArgs
    {
        public Peer Peer { get; set; }
        public byte[] Data { get; set; }
    }

    public class PeerConnectedEventArgs
    {
        public Peer Peer { get; set; }
    }

    public class PeerDisconnectedEventArgs
    {
        public IPEndPoint IPEndPoint { get; set; }
    }
}