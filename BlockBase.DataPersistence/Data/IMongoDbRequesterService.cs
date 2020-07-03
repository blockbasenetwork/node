using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.Domain.Blockchain;

namespace BlockBase.DataPersistence.Data
{
    public interface IMongoDbRequesterService
    {
        Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName);
        Task<TransactionDB> RetrievePendingTransaction(string databaseName);
        Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName);
        Task DropRequesterDatabase(string sidechain);
        Task MovePendingTransactionToExecutedAsync(string databaseName, TransactionDB transactions);
        Task AddPendingExecutionTransactionAsync(string databaseName, TransactionDB transaction);
        Task RemovePendingExecutionTransactionAsync(string databaseName, TransactionDB transaction);
        Task RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, ulong lastIncludedTransactionSequenceNumber);

    }
}