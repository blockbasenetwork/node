using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.SidechainState
{
    public class CurrentSidechainStatus
    {
        private TimeSpan _deltaBetweenRequests = TimeSpan.FromMilliseconds(100);
        private TimeSpan _deltaBetweenUpdates = TimeSpan.FromSeconds(5);
        private TimeSpan _deltaUntilCommunicationsConsideredDead = TimeSpan.FromMinutes(5);
        private const int MAX_NUMBER_OF_COMM_TRIES_TO_GIVE_UP = 20;
        private IMainchainService _mainchainService;
        private ILogger<CurrentSidechainStatus> _logger;

        public CurrentSidechainStatus(IMainchainService mainchainService, ILogger<CurrentSidechainStatus> logger)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

        public ContractStateTable ContractState { get; private set; }
        public ContractInformationTable ContractInfo { get; private set; }
        public IEnumerable<ProducerInTable> ProducerList { get; private set; }
        public IEnumerable<CandidateTable> CandidateList { get; private set; }
        public CurrentProducerTable CurrentProducer { get; private set; }


        public DateTime LastUpdateFailDate { get; private set; }
        public DateTime LastUpdateDate { get; private set; }
        public int NumUpdateFails { get; private set; }

        public bool DoesStatusNeedUpdate()
        {
            return LastUpdateDate + _deltaBetweenUpdates < DateTime.UtcNow;
        }

        public TimeSpan GetTimeSpanUntilNeedsUpdate()
        {
            return DateTime.UtcNow - (LastUpdateDate + _deltaBetweenUpdates);
        }

        public bool AreCommunicationsDead()
        {
            return NumUpdateFails > MAX_NUMBER_OF_COMM_TRIES_TO_GIVE_UP 
            || (LastUpdateDate != DateTime.MinValue && LastUpdateDate + _deltaUntilCommunicationsConsideredDead < DateTime.UtcNow);
        }


        public async Task<bool> TryUpdateSidechainStatus(string sidechainName)
        {
            try
            {
                //fetch
                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                await Task.Delay(_deltaBetweenRequests);
                var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
                await Task.Delay(_deltaBetweenRequests);
                var currentProducer = await _mainchainService.RetrieveCurrentProducer(sidechainName);
                await Task.Delay(_deltaBetweenRequests);
                var producerList = await _mainchainService.RetrieveProducersFromTable(sidechainName);
                await Task.Delay(_deltaBetweenRequests);
                var candidateList = await _mainchainService.RetrieveCandidates(sidechainName);


                //associate - we should do it in two steps because if it fails midway it doesn't update the whole table state
                ContractState = contractState;
                ContractInfo = contractInfo;
                CurrentProducer = currentProducer;
                ProducerList = producerList;
                CandidateList = candidateList;

                LastUpdateDate = DateTime.UtcNow;

                NumUpdateFails = 0;

                return true;
            }
            catch(Exception ex)
            {
                _logger.LogDebug("Unable to fetch sidechain state", ex.Message);

                LastUpdateFailDate = DateTime.UtcNow;

                NumUpdateFails++;
                return false;
            }
        }
    }

}