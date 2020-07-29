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

namespace BlockBase.DataPersistence.Data
{
    public abstract class AbstractMongoDbService
    {
        protected IMongoClient MongoClient { get; set; }
        protected ILogger _logger;
        protected string _dbPrefix;

        public AbstractMongoDbService(IOptions<NodeConfigurations> nodeConfigurations, ILogger<MongoDbProducerService> logger)
        {
            MongoClient = new MongoClient(nodeConfigurations.Value.MongoDbConnectionString);

            _logger = logger;
            _dbPrefix = nodeConfigurations.Value.DatabasesPrefix;
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
                    var myByte = (byte) c;
                    var hex = myByte.ToString("X");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }
    }
}