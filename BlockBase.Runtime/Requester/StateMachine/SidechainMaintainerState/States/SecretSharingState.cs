using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class SecretSharingState : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private IMainchainService _mainchainService;
        private ContractStateTable _contractState;

        private NodeConfigurations _nodeConfigurations;

        private ContractInformationTable _contractInfo;

        private bool _hasEnoughCandidates;

        public SecretSharingState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            
        }

        protected override async Task DoWork()
        {
            if (_hasEnoughCandidates)
                await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SECRET_TIME, _nodeConfigurations.AccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractState == null || _contractInfo == null) return Task.FromResult(false);
            if(!IsTimeUpForSidechainPhase(_contractInfo.CandidatureEndDate, 0))
                return Task.FromResult(false);
            return Task.FromResult(true);

            //return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.SecretEndDate, 0));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(_contractState.SecretTime) return Task.FromResult((true, typeof(NextStateRouter).Name));
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_contractState.SecretTime);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);

            var producers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
            var candidates = await _mainchainService.RetrieveCandidates(_nodeConfigurations.AccountName);

            var numberOfRequiredProviders = _contractInfo.NumberOfFullProducersRequired + _contractInfo.NumberOfHistoryProducersRequired + _contractInfo.NumberOfValidatorProducersRequired;

            if (producers.Count + candidates.Count >= SmartContractConstants.MIN_PRODUCERS_TO_PRODUCE * numberOfRequiredProviders && candidates.Any())
            {
                _hasEnoughCandidates = true;
            }
            else
            {
                _delay = TimeSpan.FromSeconds(15);
            }
        }
    }
}