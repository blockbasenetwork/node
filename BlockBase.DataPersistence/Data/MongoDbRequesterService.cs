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
using System;

namespace BlockBase.DataPersistence.Data
{
    public class MongoDbRequesterService : AbstractMongoDbService, IMongoDbRequesterService
    {
        public MongoDbRequesterService(IOptions<NodeConfigurations> nodeConfigurations, ILogger<MongoDbProducerService> logger) : base(nodeConfigurations, logger)
        {
        }

        public async Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            return await RetrieveTransactionsInMempool(databaseName, MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
        }

        public async Task CreateIndexes(string databaseName)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var indexModel = new CreateIndexModel<TransactionDB>(Builders<TransactionDB>.IndexKeys.Ascending(t => t.SequenceNumber));

                var pendingExecutionTransactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                var waitingTransactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_WAITING_FOR_IRREVERSIBILITY_TRANSACTIONS_COLLECTION_NAME);

                await pendingExecutionTransactionCollection.Indexes.CreateOneAsync(indexModel);
                await transactionCollection.Indexes.CreateOneAsync(indexModel);
                await waitingTransactionscol.Indexes.CreateOneAsync(indexModel);
            }
        }

        public async Task<IList<TransactionDB>> RetrievePendingTransactions(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       select t;

                return await transactionQuery.ToListAsync();
            }
        }

        public async Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       select t.SequenceNumber;

                var result = await transactionQuery.OrderByDescending(t => t).FirstOrDefaultAsync();

                if (result != 0) return Convert.ToUInt64(result);

                transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);

                transactionQuery = from t in transactionCollection.AsQueryable()
                                   select t.SequenceNumber;

                result = await transactionQuery.OrderByDescending(t => t).FirstOrDefaultAsync();

                return Convert.ToUInt64(result);
            }
        }

        public async Task DropRequesterDatabase(string sidechain)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + sidechain);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.REQUESTER_WAITING_FOR_IRREVERSIBILITY_TRANSACTIONS_COLLECTION_NAME);

                if ((await sidechainDatabase.ListCollectionsAsync()).ToList().Count() == 0)
                {
                    await MongoClient.DropDatabaseAsync(_dbPrefix + sidechain);
                }
                await session.CommitTransactionAsync();
            }
        }

        public async Task MovePendingTransactionToExecutedAsync(string databaseName, TransactionDB transaction)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var pendingExecutionTransactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                await pendingExecutionTransactionCollection.DeleteOneAsync(t => t.SequenceNumber == transaction.SequenceNumber);
                var executedTransactionsCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await executedTransactionsCollection.InsertOneAsync(transaction);
                await session.CommitTransactionAsync();
            }
        }

        public async Task MovePendingTransactionsToExecutedAsync(string databaseName, IList<TransactionDB> transactions)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            var orderedTransactions = transactions.OrderBy(t => t.SequenceNumber).ToList();
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var pendingExecutionTransactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                await pendingExecutionTransactionCollection.DeleteManyAsync(t => t.SequenceNumber >= orderedTransactions.First().SequenceNumber && t.SequenceNumber <= orderedTransactions.Last().SequenceNumber);
                var executedTransactionsCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                await executedTransactionsCollection.InsertManyAsync(transactions);
                await session.CommitTransactionAsync();
            }
        }

        public async Task AddPendingExecutionTransactionsAsync(string databaseName, IList<TransactionDB> transactions)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionsCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                foreach (var transaction in transactions) await transactionsCollection.InsertOneAsync(transaction);
                await session.CommitTransactionAsync();
            }
        }

        public async Task RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, ulong lastIncludedTransactionSequenceNumber)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                var transactionQuery = from t in transactionscol.AsQueryable()
                                       where t.SequenceNumber <= Convert.ToInt64(lastIncludedTransactionSequenceNumber)
                                       select t;
                var waitingTransactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_WAITING_FOR_IRREVERSIBILITY_TRANSACTIONS_COLLECTION_NAME);
                var transactionsToRemove = await transactionQuery.ToListAsync();
                if (transactionsToRemove.Any()) await waitingTransactionscol.InsertManyAsync(transactionsToRemove);
                await transactionscol.DeleteManyAsync(t => t.SequenceNumber <= Convert.ToInt64(lastIncludedTransactionSequenceNumber));
            }
        }

        public async Task<IList<Transaction>> RollbackAndRetrieveWaitingTransactions(string databaseName, ulong lastIncludedTransactionSequenceNumber)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var waitingTransactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_WAITING_FOR_IRREVERSIBILITY_TRANSACTIONS_COLLECTION_NAME);
                var transactionQuery = from t in waitingTransactionscol.AsQueryable()
                                       where t.SequenceNumber > Convert.ToInt64(lastIncludedTransactionSequenceNumber)
                                       select t;
                var transactionToSend = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_TRANSACTIONS_COLLECTION_NAME);
                var transactionsToRetrieve = await transactionQuery.ToListAsync();
                if (transactionsToRetrieve.Any()) await transactionToSend.InsertManyAsync(transactionsToRetrieve);
                await waitingTransactionscol.DeleteManyAsync(t => t.SequenceNumber > Convert.ToInt64(lastIncludedTransactionSequenceNumber));
                return transactionsToRetrieve.Select(t => t.TransactionFromTransactionDB()).ToList();
            }
        }

        public async Task RemovePendingExecutionTransactionAsync(string databaseName, TransactionDB transaction)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                await transactionscol.DeleteOneAsync(t => t.TransactionHash == transaction.TransactionHash);
                var update = Builders<TransactionDB>.Update.Inc("SequenceNumber", -1);
                await transactionscol.UpdateManyAsync(t => t.SequenceNumber > transaction.SequenceNumber, update);
            }
        }

        public async Task RemovePendingExecutionTransactionsAsync(string databaseName, IList<TransactionDB> transactions)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            var orderedTransactions = transactions.OrderBy(t => t.SequenceNumber).ToList();
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var transactionscol = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME);
                await transactionscol.DeleteManyAsync(t => t.SequenceNumber >= orderedTransactions.First().SequenceNumber && t.SequenceNumber <= orderedTransactions.Last().SequenceNumber);
                var update = Builders<TransactionDB>.Update.Inc("SequenceNumber", -1);
                await transactionscol.UpdateManyAsync(t => t.SequenceNumber > orderedTransactions.Last().SequenceNumber, update);
            }
        }
    }
}
