using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class NodeConfigurations
    {
        public string AccountName { get; set; }
        public string ActivePrivateKey { get; set; }
        public string ActivePublicKey { get; set; }
        public string SecretPassword { get; set; }
        public string MySqlServer { get; set; }
        public int MySqlPort { get; set; }
        public string MySqlUser { get; set; }
        public string MySqlPassword { get; set; }
        public string MongoDbConnectionString { get; set; }
        public string MongoDbPrefix { get; set; }
    }
}
