using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Utils
{
    public interface IConnectionsChecker
    {
        Task<bool> IsAbleToConnectToMongoDb();

        Task<bool> IsAbleToConnectToPostgres();
    }
}