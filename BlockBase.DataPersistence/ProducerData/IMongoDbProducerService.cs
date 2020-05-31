using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;

namespace BlockBase.DataPersistence.ProducerData
{
    public interface IMongoDbProducerService
    {
        Task CreateDatabasesAndIndexes(string databaseName);
        Task CreateTransactionInfoIfNotExists(string databaseName);
        Task<IList<TransactionDB>> GetTransactionsByBlockSequenceNumberAsync(string databaseName, ulong blockSequence);
        Task AddTransactionsToSidechainDatabaseAsync(string databaseName, TransactionDB transaction);
        Task AddTransactionsToSidechainDatabaseAsync(string databaseName, IEnumerable<TransactionDB> transactions);
        Task AddBlockToSidechainDatabaseAsync(Block block, string databaseName);
        Task<Block> GetLastValidSidechainBlockAsync(string databaseName);
        Task<Block> GetSidechainBlockAsync(string sidechain, string blockhash);
        Task<IList<Block>> GetSidechainBlocksSinceSequenceNumberAsync(string databaseName, ulong beginSequenceNumber, ulong endSequenceNumber);
        Task<IEnumerable<ulong>> GetMissingBlockNumbers(string databaseName, ulong endSequenceNumber);
        Task RemoveBlockFromDatabaseAsync(string databaseName, string blockHash);
        Task RemoveUnconfirmedBlocks(string databaseName);
        Task<bool> SynchronizeDatabaseWithSmartContract(string databaseName, string blockHash, long lastProductionTime);
        Task<bool> IsBlockConfirmed(string databaseName, string blockHash);
        Task ConfirmBlock(string databaseName, string blockHash);
        Task<bool> IsTransactionInDB(string databaseName, Transaction transaction);
        Task<bool> IsBlockInDatabase(string databaseName, string blockhash);
        Task SaveTransaction(string databaseName, Transaction transaction);
        Task<IList<Transaction>> RetrieveLastLooseTransactions(string databaseName);
        Task<Transaction> LastIncludedTransaction(string databaseName);
        Task<IList<Transaction>> GetBlockTransactionsAsync(string databaseName, string blockhash);
        Task<Transaction> GetTransactionBySequenceNumber(string databaseName, ulong transactionNumber);
        Task<IList<Transaction>> GetTransactionsSinceSequenceNumber(string databaseName, ulong transactionNumber);
        Task<TransactionDB> GetTransactionDBAsync(string databaseName, string transactionHash);
        Task<ulong> GetLastTransactionSequenceNumberDBAsync(string databaseName);
        Task<IList<ulong>> RemoveAlreadyIncludedTransactionsDBAsync(string databaseName, uint numberOfIncludedTransactions, string lastValidBlockHash);
        Task AddProducingSidechainToDatabaseAsync(string sidechain);
        Task RemoveProducingSidechainFromDatabaseAsync(string sidechain);
        Task<bool> CheckIfProducingSidechainAlreadyExists(string sidechain);
        Task<IList<SidechainDB>> GetAllProducingSidechainsAsync();
        Task AddMaintainedSidechainToDatabaseAsync(string sidechain);
        Task RemoveMaintainedSidechainFromDatabaseAsync(string sidechain);
        Task<bool> CheckIfMaintainedSidechainAlreadyExists(string sidechain);
        Task<IList<SidechainDB>> GetAllMaintainedSidechainsAsync();
    }
}