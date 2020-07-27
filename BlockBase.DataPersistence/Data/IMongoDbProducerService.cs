using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.Domain.Enums;

namespace BlockBase.DataPersistence.Data
{
    public interface IMongoDbProducerService
    {
        Task AddBlockToSidechainDatabaseAsync(Block block, string databaseName);
        Task<Block> GetSidechainBlockAsync(string sidechain, string blockhash);
        Task<IList<Block>> GetSidechainBlocksSinceSequenceNumberAsync(string databaseName, ulong beginSequenceNumber, ulong endSequenceNumber);
        Task<IEnumerable<ulong>> GetMissingBlockNumbers(string databaseName, ulong endSequenceNumber);
        Task RemoveBlockFromDatabaseAsync(string databaseName, string blockHash);
        Task RemoveUnconfirmedBlocks(string databaseName);
        Task<bool> TrySynchronizeDatabaseWithSmartContract(string databaseName, string blockHash, long lastProductionTime, ProducerTypeEnum producerType);
        Task<bool> IsBlockConfirmed(string databaseName, string blockHash);
        Task ConfirmBlock(string databaseName, string blockHash);
        Task ClearValidatorNode(string databaseName, string blockHash, uint transactionCount);
        Task<bool> IsTransactionInDB(string databaseName, Transaction transaction);
        Task<bool> IsBlockInDatabase(string databaseName, string blockhash);
        Task SaveTransaction(string databaseName, Transaction transaction);
        Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName);
        Task<IList<Transaction>> GetBlockTransactionsAsync(string databaseName, string blockhash);
        Task<Transaction> GetTransactionBySequenceNumber(string databaseName, ulong transactionNumber);
        Task<IList<Transaction>> GetTransactionsSinceSequenceNumber(string databaseName, ulong transactionNumber);
        Task<TransactionDB> GetTransactionDBAsync(string databaseName, string transactionHash);
        
        Task AddProducingSidechainToDatabaseAsync(string sidechain, ulong timestamp, bool isAutomatic);
        Task RemoveProducingSidechainFromDatabaseAsync(string sidechain);
        Task<bool> CheckIfProducingSidechainAlreadyExists(string sidechain);
        Task<IList<SidechainDB>> GetAllProducingSidechainsAsync();
        Task<SidechainDB> GetProducingSidechainAsync(string sidechain, ulong timestamp);

        Task AddPastSidechainToDatabaseAsync(string sidechain, ulong timestamp, bool alreadyLeft = false, string reasonLeft = null);
        Task RemovePastSidechainFromDatabaseAsync(string sidechain, ulong timestamp);
        Task<IList<PastSidechainDB>> GetAllPastSidechainsAsync();
        Task<PastSidechainDB> GetPastSidechainAsync(string sidechain, ulong timestamp);

        Task<TransactionDB> GetTransactionToExecute(string sidechain);
        Task UpdateTransactionToExecute(string sidechain, TransactionDB transaction);
    }
}