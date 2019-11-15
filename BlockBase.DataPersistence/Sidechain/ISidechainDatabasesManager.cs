using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Database.Operations;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;

namespace BlockBase.DataPersistence.Sidechain
{
    public interface ISidechainDatabasesManager
    {
        bool CheckMetaInfoExist(string databaseName);
        void Enqueue(ISqlOperation operation, string databaseName);
        Task CreateDatabase(string databaseName);
        void CreateMetaInfo(string databaseName);
        Task<bool> CheckDatabase(string databaseName);
        Task Execute();
        void ExecuteBlockTransactions(IList<Transaction> blockTransactions);       
        void Build<T>(Transaction transaction) where T : ISqlOperation;

    }

}