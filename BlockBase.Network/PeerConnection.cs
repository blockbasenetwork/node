using Open.P2P;
using System.Net;

namespace BlockBase.Network
{
    public class PeerConnection
    {
        public IPEndPoint IPEndPoint { get; set; }

        public string PublicKey { get; set; }

        public int Rating { get; set; }

        public Peer Peer { get; set; }

        public ConnectionStateEnum ConnectionState { get; set; }

        public enum ConnectionStateEnum
        {
            Unknown,
            Connected,
            ConnectionRequested,
            Disconnected
        }

        public PeerConnection()
        {
            ConnectionState = ConnectionStateEnum.Unknown;
        }

        //added by marciak
        public PeerConnection(int rating, Peer peer, ConnectionStateEnum connectionState) {
            Rating = rating;
            Peer = peer;
            ConnectionState = connectionState;
        }
    }
}