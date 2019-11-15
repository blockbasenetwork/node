using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;

namespace BlockBase.Utils
{
    public class SystemConfig : ICloneable
    {
        public IPAddress IPAddress { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }
        public bool HasSystemStarted { get; set; }

        public SystemConfig(IPAddress ipAddress, int tcpPort)
        {
            IPAddress = ipAddress;
            TcpPort = tcpPort;
        }

        public object Clone()
        {
           var clone = new SystemConfig(IPAddress, TcpPort);
           clone.IPAddress = this.IPAddress;
           clone.HasSystemStarted = this.HasSystemStarted;
           clone.UdpPort = this.UdpPort;
           clone.TcpPort = this.TcpPort;
           return clone;
        }
    }
}