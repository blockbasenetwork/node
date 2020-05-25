

using System;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlockBase.DataPersistence.Utils
{
    public class ConnectionsChecker : IConnectionsChecker
    {
        private IConnector _connector;
        private IMongoClient _mongoClient;
        public ConnectionsChecker(IConnector connector, IOptions<NodeConfigurations> nodeConfigurations)
        {
            _connector = connector;
            _mongoClient = new MongoClient(nodeConfigurations.Value.MongoDbConnectionString);
        }

        public async Task<bool> IsAbleToConnectToMongoDb() 
        {
            IClientSessionHandle handle = null;

            try
            {
                handle = await _mongoClient.StartSessionAsync();

                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public async Task<bool> IsAbleToConnectToPostgres() 
        {
            return await _connector.TestConnection();
        }
    }
}