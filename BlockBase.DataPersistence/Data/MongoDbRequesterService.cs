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
        public async Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName)
        {
            return await RetrieveTransactionsInMempool(databaseName, MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
        }
       
        public async Task<IList<TransactionDB>> RetrievePendingTransactions(string databaseName)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_TO_EXECUTE_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       select t;

                return await transactionQuery.ToListAsync();
            }
        }

        public async Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_TO_EXECUTE_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       select t.SequenceNumber;

                var result = await transactionQuery.OrderByDescending(t => t).FirstOrDefaultAsync();

                if(result != 0) return result;
                
                transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);

                transactionQuery = from t in transactionCollection.AsQueryable()
                                       select t.SequenceNumber;

                return await transactionQuery.OrderByDescending(t => t).FirstOrDefaultAsync();
            }
        }

        public async Task DropRequesterDatabase(string sidechain)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + sidechain);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.REQUESTER_TRANSACTIONS_TO_EXECUTE_COLLECTION_NAME);

                if ((await sidechainDatabase.ListCollectionsAsync()).ToList().Count() == 0)
                {
                    await MongoClient.DropDatabaseAsync(_dbPrefix + sidechain);
                }
            }
        }

        public async Task MovePendingTransactionToExecutedAsync(string databaseName, TransactionDB transaction)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var pendingExecutionTransactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_TO_EXECUTE_COLLECTION_NAME);
                await pendingExecutionTransactionCollection.DeleteOneAsync(t=> transaction.TransactionHash == transaction.TransactionHash);
                var executedTransactionsCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await executedTransactionsCollection.InsertOneAsync(transaction);
                await session.CommitTransactionAsync();
            }
        }

        public async Task AddPendingExecutionTransactionsAsync(string databaseName, IList<TransactionDB> transactions)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionsCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_TO_EXECUTE_COLLECTION_NAME);
                foreach(var transaction in transactions) await transactionsCollection.InsertOneAsync(transaction);
                await session.CommitTransactionAsync();
            }
        }

        public async Task RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, ulong lastIncludedTransactionSequenceNumber)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await transactionscol.DeleteManyAsync(t => t.SequenceNumber <= lastIncludedTransactionSequenceNumber);
            }
        }

        public async Task RemovePendingExecutionTransactionAsync(string databaseName, TransactionDB transaction)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_TO_EXECUTE_COLLECTION_NAME);
                await transactionscol.DeleteOneAsync(t => t.TransactionHash == transaction.TransactionHash);
            }
        }
    }
}
