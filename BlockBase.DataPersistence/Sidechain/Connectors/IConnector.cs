using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public interface IConnector
    {
        Task<IList<string>> GetDatabasesList();
        Task<IList<InfoRecord>> GetInfoRecords(string databaseName);
    }
}