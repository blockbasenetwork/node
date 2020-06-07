using System;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class NextStateRouter : AbstractMainchainState<StartState, EndState>
    {
        private string _nextState;
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private CurrentProducerTable _currentProducer;
        private IMainchainService _mainchainService;
        private SidechainPool _sidechainPool;

        public NextStateRouter(ILogger logger, SidechainPool sidechainPool, IMainchainService mainchainService) : base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(true);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((true, _nextState));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(false);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);

            _nextState = GetNextSidechainState(_contractInfo, _contractState, _currentProducer, _sidechainPool);

            if(_nextState == null)
            {
                _logger.LogDebug($"{this.GetType().Name} - Nothing to do to maintain...delaying");
                _delay = TimeSpan.FromSeconds(3);
            }
        }

        private string GetNextSidechainState(ContractInformationTable contractInfo, ContractStateTable contractState, CurrentProducerTable currentProducer, SidechainPool sidechainPool)
        {
            if(contractState.ConfigTime) 
                return typeof(CandidatureReceivalState).Name;
            if(contractState.CandidatureTime && IsTimeUpForSidechainPhase(contractInfo.CandidatureEndDate, 0))
                return typeof(SecretSharingState).Name;
            if(contractState.SecretTime && IsTimeUpForSidechainPhase(contractInfo.SecretEndDate, 0))
                return typeof(IPSharingState).Name;
            if(contractState.IPSendTime && IsTimeUpForSidechainPhase(contractInfo.SendEndDate, 0))
                return typeof(ProvidersConnectionState).Name;
            return null;
        }

        private TimeSpan CalculateNextDelay()
        {
            return TimeSpan.FromMinutes(1);
            //TODO
        }
    }
}