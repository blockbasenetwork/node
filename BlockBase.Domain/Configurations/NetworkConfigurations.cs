using System;
using System.Collections.Generic;
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
            return this.PublicIpAddress + ":" + this.TcpPort;
        }
    }
}
