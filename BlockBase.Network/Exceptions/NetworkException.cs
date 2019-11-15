using BlockBase.Network.IO;
using System;

namespace BlockBase.Network.Exceptions
{
    public class NetworkException : Exception
    {
        public NetworkMessage NetworkMessage { get; set; }

        public NetworkException(NetworkMessage networkMessage)
        {
            NetworkMessage = networkMessage;
        }
    }
}