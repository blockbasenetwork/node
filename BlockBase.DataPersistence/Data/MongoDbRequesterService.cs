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

namespace BlockBase.DataPersistence.Data
{
    public class MongoDbRequesterService : AbstractMongoDbService, IMongoDbRequesterService
    {
        public MongoDbRequesterService(IOptions<NodeConfigurations> nodeConfigurations, ILogger<MongoDbProducerService> logger) : base(nodeConfigurations, logger)
        {
        }
        public async Task<IList<ulong>> RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, uint numberOfIncludedTransactions, string lastValidBlockHash)
        {

            var lastTransactionSequenceNumber = await GetLastTransactionSequenceNumberDBAsync(databaseName);
            var lastIncludedSequenceNumber = lastTransactionSequenceNumber + (ulong)numberOfIncludedTransactions;
            IList<ulong> sequenceNumbers = new List<ulong>();
            for (ulong i = lastTransactionSequenceNumber + 1; i <= lastIncludedSequenceNumber; i++)
                sequenceNumbers.Add(i);

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionInfoCollection = sidechainDatabase.GetCollection<TransactionInfoDB>(MongoDbConstants.TRANSACTIONS_INFO_COLLECTION_NAME);
                var info = await (await transactionInfoCollection.FindAsync(t => true)).SingleOrDefaultAsync();
                //TODO rpinto - this assumes that the requester has received the block from the providers - this may not always happen?
                if (info.BlockHash == lastValidBlockHash) return sequenceNumbers;

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);

                session.StartTransaction();
                await transactionCollection.DeleteManyAsync(s => sequenceNumbers.Contains(s.SequenceNumber));
                await transactionInfoCollection.DeleteManyAsync(t => true);
                await transactionInfoCollection.InsertOneAsync(new TransactionInfoDB() { BlockHash = lastValidBlockHash, LastIncludedSequenceNumber = lastIncludedSequenceNumber });
                await session.CommitTransactionAsync();
            }

            return sequenceNumbers;
        }

        public async Task CreateTransactionInfoIfNotExists(string databaseName)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {


                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionInfoCollection = sidechainDatabase.GetCollection<TransactionInfoDB>(MongoDbConstants.TRANSACTIONS_INFO_COLLECTION_NAME);

                var info = await (await transactionInfoCollection.FindAsync(t => true)).SingleOrDefaultAsync();

                if (info == null)
                {
                    await transactionInfoCollection.InsertOneAsync(new TransactionInfoDB { BlockHash = "none", LastIncludedSequenceNumber = 0 });
                }
            }
        }

        public async Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName)
        {
            return await RetrieveTransactionsInMempool(databaseName, MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
        }

        public async Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionInfoCollection = sidechainDatabase.GetCollection<TransactionInfoDB>(MongoDbConstants.TRANSACTIONS_INFO_COLLECTION_NAME);

                var transactionQuery = from t in transactionInfoCollection.AsQueryable()
                                       select t.LastIncludedSequenceNumber;

                return await transactionQuery.SingleOrDefaultAsync();
            }
        }

        public async Task DropRequesterDatabase(string sidechain)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + sidechain);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.TRANSACTIONS_INFO_COLLECTION_NAME);

                if ((await sidechainDatabase.ListCollectionsAsync()).ToList().Count() == 0)
                {
                    await MongoClient.DropDatabaseAsync(_dbPrefix + sidechain);
                }
            }
        }

        public async Task AddTransactionsToSidechainDatabaseAsync(string databaseName, IEnumerable<TransactionDB> transactions)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await transactionscol.InsertManyAsync(transactions);
                await session.CommitTransactionAsync();
            }
        }
    }
}
