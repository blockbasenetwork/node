using System;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Mainchain.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.SidechainProductionState.States
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

            var nextState = GetNextSidechainState(_contractInfo, _contractState, _currentProducer, _sidechainPool);

            //TODO this should do a delay
            if(nextState == null)
            {
                _delay = TimeSpan.FromSeconds(3);
            }
        }

        private string GetNextSidechainState(ContractInformationTable contractInfo, ContractStateTable contractState, CurrentProducerTable currentProducer, SidechainPool sidechainPool)
        {
            if(contractState.IPReceiveTime && IsTimeUpForSidechainPhase(contractInfo.ReceiveEndDate, 0))
                return typeof(StartProductionState).Name;

            //this works parallel to the remaining states, so perhaps it would be best to do this check in parallel to the ones above, and if
            //it turns out true to give it priority.
            //another alternative is to have a state machine just to manage production
            if(contractState.ProductionTime && currentProducer != null && IsTimeUpForSidechainPhase(currentProducer.StartProductionTime + sidechainPool.BlockTimeDuration, 0))
                return typeof(SwitchProducerTurn).Name;
            
            
            return null;
        }

        private TimeSpan CalculateNextDelay()
        {
            return TimeSpan.FromMinutes(1);
            //TODO
        }
    }
}