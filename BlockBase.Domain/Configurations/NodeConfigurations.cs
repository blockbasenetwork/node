using System;

namespace BlockBase.Domain.Configurations
{
    public class NodeConfigurations
    {
        private string _secretPassword = Guid.NewGuid().ToString();
        public string AccountName { get; set; }
        public string ActivePrivateKey { get; set; }
        public string ActivePublicKey { get; set; }
        public string SecretPassword 
        { 
            get { return _secretPassword; }
        }
        public string MongoDbConnectionString { get; set; }
        public string DatabasesPrefix { get; set; }
        public string PostgresHost { get; set; }
        public string PostgresUser { get; set; }
        public int PostgresPort { get; set; }
        public string PostgresPassword { get; set; }
    }
}