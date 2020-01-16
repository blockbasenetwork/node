using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public interface IConnector
    {
        Task<IList<InfoRecord>> GetInfoRecords(string databaseName);
        Task ExecuteCommand(string sqlCommand, string databaseName);
        Task<IList<IList<string>>> ExecuteQuery(string sqlQuery, string databaseName);
    }
}