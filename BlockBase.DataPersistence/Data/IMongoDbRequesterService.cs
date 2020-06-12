using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.Domain.Blockchain;

namespace BlockBase.DataPersistence.Data
{
    public interface IMongoDbRequesterService
    {
        Task CreateTransactionInfoIfNotExists(string databaseName);
        Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName);
        Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName);
        Task DropRequesterDatabase(string sidechain);
        Task AddTransactionsToSidechainDatabaseAsync(string databaseName, IEnumerable<TransactionDB> transactions);
        Task<IList<ulong>> RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, uint numberOfIncludedTransactions, string lastValidBlockHash);

    }
}