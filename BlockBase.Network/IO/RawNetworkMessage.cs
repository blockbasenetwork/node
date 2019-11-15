using System.Net;

namespace BlockBase.Network.IO
{
    public class RawNetworkMessage
    {
        public IPEndPoint IPEndPoint { get; set; }
        public byte[] Bytes { get; set; }
    }
}