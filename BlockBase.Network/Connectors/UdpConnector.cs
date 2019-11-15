using BlockBase.Utils;
using Open.P2P.EventArgs;
using Open.P2P.Listeners;
using System.Net;
using System.Net.Sockets;

namespace BlockBase.Network.Connectors
{
    public class UdpConnector
    {
        private UdpListener _discovery;

        private readonly SystemConfig _systemConfig;

        public UdpConnector(SystemConfig systemConfig)
        {
            _systemConfig = systemConfig;
        }

        public void Start()
        {
            _discovery = new UdpListener(_systemConfig.UdpPort);
            _discovery.UdpPacketReceived += DiscoveryOnUdpPacketReceived;
            _discovery.Start();
        }

        public void SendData(byte[] data, IPEndPoint endpoint)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.EnableBroadcast = true;
                var group = endpoint;
                socket.SendTo(data, group);
                socket.Close();
            }
        }

        private void DiscoveryOnUdpPacketReceived(object sender, UdpPacketReceivedEventArgs args)
        {
            DataReceived?.Invoke(this, args);
        }

        public event DataReceivedEventHandler DataReceived;

        public delegate void DataReceivedEventHandler(object sender, UdpPacketReceivedEventArgs args);
    }
}