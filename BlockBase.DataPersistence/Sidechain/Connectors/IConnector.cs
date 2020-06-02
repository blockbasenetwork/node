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
        Task<IList<IList<string>>> ExecuteQuery(string sqlQuery, string databaseName);
        Task InsertToDatabasesTable(string databaseName);
        Task DeleteFromDatabasesTable(string databaseName);
        Task DropDefaultDatabase();
        Task DropDatabase(string databaseName);
    }
}