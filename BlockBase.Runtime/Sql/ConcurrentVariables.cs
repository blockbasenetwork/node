using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Options;

namespace BlockBase.Runtime.Sql
{
    public class ConcurrentVariables
    {
        public ConcurrentDictionary<string, SemaphoreSlim> DatabasesSemaphores;
        private ulong _transactionNumber;
        private readonly object locker = new object();

        private bool _hasLoadedData = false;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private NodeConfigurations _nodeConfigurations;

        public ConcurrentVariables(IMongoDbRequesterService mongoDbRequesterService, IOptions<NodeConfigurations> nodeConfigurations)
        {
            DatabasesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _mongoDbRequesterService = mongoDbRequesterService;
            _nodeConfigurations = nodeConfigurations.Value;
        }

        public void Reset()
        {
            _hasLoadedData = false;
        }


        public ulong GetNextTransactionNumber()
        {
            lock (locker)
            {
                if(!_hasLoadedData)
                {
                    LoadTransactionNumberFromDB().Wait();
                    _hasLoadedData = true;
                }
                _transactionNumber++;
                return _transactionNumber;
            }
        }

        private async Task LoadTransactionNumberFromDB()
        {
            _transactionNumber = await _mongoDbRequesterService.GetLastTransactionSequenceNumberDBAsync(_nodeConfigurations.AccountName);
        }
    }
}