using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public interface IConnector
    {

        Task<bool> TestConnection();
        Task Setup();
        Task<IList<InfoRecord>> GetInfoRecords();
        Task ExecuteCommand(string sqlCommand, string databaseName);
        Task ExecuteCommandWithTransactionNumber(string sqlCommand, string databaseName, ulong transactionNumer);
        Task<bool> WasTransactionExecuted(string databaseName, ulong transactionNumer);
        Task<IList<IList<string>>> ExecuteQuery(string sqlQuery, string databaseName);
        Task InsertToDatabasesTable(string databaseName);
        Task DeleteFromDatabasesTable(string databaseName);
        Task DropDefaultDatabase();
        Task DropDatabase(string databaseName);
        Task<bool> DoesDefaultDatabaseExist();
        Task<bool> DoesDatabaseExist(string databaseName);
    }
}