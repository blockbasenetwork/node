using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Driver.Linq;
using BlockBase.Domain.Blockchain;
using System.Text;
using System;
using MongoDB.Bson;

namespace BlockBase.DataPersistence.Data
{
    public abstract class AbstractMongoDbService
    {
        protected IMongoClient MongoClient { get; set; }
        protected ILogger _logger;
        protected string _dbPrefix;

        protected AbstractMongoDbService(IOptions<NodeConfigurations> nodeConfigurations, ILogger<MongoDbProducerService> logger)
        {
            MongoClient = new MongoClient(nodeConfigurations.Value.MongoDbConnectionString);

            _logger = logger;
            _dbPrefix = nodeConfigurations.Value.DatabasesPrefix;
        }

        public async Task AddBBTValueToDatabaseAsync(double BBTValue)
        {
            var bbtValueDb = new BBTValueDB()
            {
                Timestamp = Convert.ToUInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ValueInUSD = BBTValue
            };

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var sidechainCollection = recoverDatabase.GetCollection<BBTValueDB>(MongoDbConstants.BBT_VALUE_COLLETION_NAME);
                await sidechainCollection.InsertOneAsync(bbtValueDb);
            }
        }

        public async Task<BBTValueDB> GetLatestBBTValue()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var bbtValuesCol = database.GetCollection<BBTValueDB>(MongoDbConstants.BBT_VALUE_COLLETION_NAME).AsQueryable();
                var query = from t in bbtValuesCol
                            orderby t.Timestamp descending
                            select t;

                return query.FirstOrDefault();
            }
        }

        public async Task<BBTValueDB> GetPreviousWeekBBTValue()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var bbtValuesCol = database.GetCollection<BBTValueDB>(MongoDbConstants.BBT_VALUE_COLLETION_NAME).AsQueryable();
                var query = from t in bbtValuesCol
                            where t.Timestamp < Convert.ToUInt64(DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds())
                            orderby t.Timestamp descending
                            select t;

                return query.FirstOrDefault();
            }
        }

        protected async Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName, string collection)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(collection);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       where t.BlockHash == ""
                                       orderby t.SequenceNumber
                                       select t;

                var transactionDBList = await transactionQuery.ToListAsync();

                return transactionDBList.Select(t => t.TransactionFromTransactionDB()).ToList();
            }
        }

        protected string ClearSpecialCharacters(string accountName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in accountName)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
                else
                {
                    var myByte = (byte)c;
                    var hex = myByte.ToString("X");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }

        protected bool IsReplicaSet()
        {
            return MongoClient.Cluster.Description.Type == MongoDB.Driver.Core.Clusters.ClusterType.ReplicaSet;
        }

        protected async Task<bool> CollectionExistsAsync(string databaseName, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await MongoClient.GetDatabase(_dbPrefix + databaseName).ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }
    }
}