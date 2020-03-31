using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Options;

namespace BlockBase.Runtime
{
    public class ConcurrentVariables
    {
        public ConcurrentDictionary<string, SemaphoreSlim> DatabasesSemaphores;
        private ulong _transactionNumber;
        private readonly object locker = new object();
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;

        public ConcurrentVariables(IMongoDbProducerService mongoDbProducerService, IOptions<NodeConfigurations> nodeConfigurations)
        {
            DatabasesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _mongoDbProducerService = mongoDbProducerService;
            _nodeConfigurations = nodeConfigurations.Value;
            LoadTransactionNumberFromDB().Wait();
        }

        public ulong GetNextTransactionNumber()
        {
            lock (locker)
            {
                _transactionNumber++;
                return _transactionNumber;
            }
        }

        private async Task LoadTransactionNumberFromDB()
        {
            _transactionNumber = await _mongoDbProducerService.GetLastTransactionSequenceNumberDBAsync(_nodeConfigurations.AccountName);
        }
    }
}