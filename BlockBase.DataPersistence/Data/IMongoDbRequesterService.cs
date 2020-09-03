using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.Domain.Blockchain;

namespace BlockBase.DataPersistence.Data
{
    public interface IMongoDbRequesterService
    {
        Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName);
        Task CreateIndexes(string databaseName);
        Task<IList<TransactionDB>> RetrievePendingTransactions(string databaseName);
        Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName);
        Task DropRequesterDatabase(string sidechain);
        Task MovePendingTransactionToExecutedAsync(string databaseName, TransactionDB transactions);
        Task MovePendingTransactionsToExecutedAsync(string databaseName, IList<TransactionDB> transactions);
        Task AddPendingExecutionTransactionsAsync(string databaseName, IList<TransactionDB> transaction);
        Task RemovePendingExecutionTransactionAsync(string databaseName, TransactionDB transaction);
        Task RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, ulong lastIncludedTransactionSequenceNumber);
        Task<IList<Transaction>> RollbackAndRetrieveWaitingTransactions(string databaseName, ulong lastIncludedTransactionSequenceNumber);
        Task RemovePendingExecutionTransactionsAsync(string databaseName, IList<TransactionDB> transactions);

    }
}