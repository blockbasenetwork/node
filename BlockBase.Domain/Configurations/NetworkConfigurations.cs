using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class NetworkConfigurations
    {
        public string PublicIpAddress { get; set; }
        public int TcpPort { get; set; }
        public List<string> EosNetworks { get; set; }
        public uint ConnectionExpirationTimeInSeconds { get; set; }
        public int MaxNumberOfConnectionRetries { get; set; }
        public string BlockBaseOperationsContract { get; set; }
        public string BlockBaseTokenContract { get; set; }

        public string GetEndPoint()
        {
            return this.GetResolvedIp() + ":" + this.TcpPort;
        }

        public string GetResolvedIp()
        {
            if (IPAddress.TryParse(PublicIpAddress, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return PublicIpAddress;
            
            var resolvedIp = Dns.GetHostEntry(PublicIpAddress).AddressList.First(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return resolvedIp?.ToString() ?? PublicIpAddress;
        }
    }
}
