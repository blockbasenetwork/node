using BlockBase.Domain.Database.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public interface IConnector
    {
        Task Execute(Dictionary<string, LinkedList<ISqlOperation>> commandsToExecute);

        Task CreateMetaInfo(string databaseName);

        Task<bool> CheckDatabase(string databaseName);

        Task CreateDatabase(string databaseName);

        bool CheckIfMetaInfoTableExist(string databaseName);
    }
}