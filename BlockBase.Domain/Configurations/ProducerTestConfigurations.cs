using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class ProducerTestConfigurations
    {
        public string ClientAccountName { get; set; }
        public string ClientAccountPrivateKey { get; set; }
        public string ClientAccountPublicKey { get; set; }

        public string Producer1Name { get; set; }
        public string Producer1PrivateKey { get; set; }
        public string Producer1PublicKey { get; set; }

        public string Producer2Name { get; set; }
        public string Producer2PrivateKey { get; set; }
        public string Producer2PublicKey { get; set; }

        public string Producer3Name { get; set; }
        public string Producer3PrivateKey { get; set; }
        public string Producer3PublicKey { get; set; }

        public string LocalNet { get; set; }
    }
}
