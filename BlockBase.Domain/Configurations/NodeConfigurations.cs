namespace BlockBase.Domain.Configurations
{
    public class NodeConfigurations
    {
        public string AccountName { get; set; }
        public string ActivePrivateKey { get; set; }
        public string ActivePublicKey { get; set; }
        public string SecretPassword { get; set; }
        public string MongoDbConnectionString { get; set; }
        public string MongoDbPrefix { get; set; }
        public string PostgresHost { get; set; }
        public string PostgresUser { get; set; }
        public int PostgresPort { get; set; }
        public string PostgresPassword { get; set; }
    }
}