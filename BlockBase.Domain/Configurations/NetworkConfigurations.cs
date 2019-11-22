using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class NetworkConfigurations
    {
        public string LocalIpAddress { get; set; }
        public int LocalTcpPort { get; set; }
        public string EosNet { get; set; }
        public uint ConnectionExpirationTimeInSeconds { get; set; }
        public int MaxNumberOfConnectionRetries { get; set; }
        public string BlockBaseOperationsContract { get; set; }
        public string BlockBaseTokenContract { get; set; }
    }
}
