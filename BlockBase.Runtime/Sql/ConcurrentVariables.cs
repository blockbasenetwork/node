using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
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
        private IMainchainService _mainchainService;

        public ConcurrentVariables(IMongoDbRequesterService mongoDbRequesterService, IOptions<NodeConfigurations> nodeConfigurations, IMainchainService mainchainService)
        {
            DatabasesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _mongoDbRequesterService = mongoDbRequesterService;
            _nodeConfigurations = nodeConfigurations.Value;
            _mainchainService = mainchainService;
        }

        public void Reset()
        {
            _hasLoadedData = false;
        }


        public ulong GetNextTransactionNumber()
        {
            lock (locker)
            {
                if (!_hasLoadedData)
                {
                    LoadTransactionNumberFromDB().Wait();
                    _hasLoadedData = true;
                }
                _transactionNumber++;
                return _transactionNumber;
            }
        }
        
        public ulong ReloadTransactionNumber()
        {
            lock (locker)
            {
                LoadTransactionNumberFromDB().Wait();
                return _transactionNumber;
            }
        }

        private async Task LoadTransactionNumberFromDB()
        {
            var transactionNumber = await _mongoDbRequesterService.GetLastTransactionSequenceNumberDBAsync(_nodeConfigurations.AccountName);
            if (transactionNumber == 0)
            {
                var contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);
                transactionNumber = (await _mainchainService.GetLastValidSubmittedBlockheader(_nodeConfigurations.AccountName, (int) contractInfo.BlocksBetweenSettlement))?.LastTransactionSequenceNumber ?? 0;
            }
            _transactionNumber = transactionNumber;
        }
    }
}