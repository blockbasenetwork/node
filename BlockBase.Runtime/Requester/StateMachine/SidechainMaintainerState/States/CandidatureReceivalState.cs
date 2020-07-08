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
    public class CandidatureReceivalState : AbstractMainchainState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private ContractInformationTable _contractInfo;

        private NodeConfigurations _nodeConfigurations;

        private ContractStateTable _contractState;
        public CandidatureReceivalState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override async Task DoWork()
        {
            await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_CANDIDATURE_TIME, _nodeConfigurations.AccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractInfo != null);
            //return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.CandidatureEndDate, 0));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(_contractState.CandidatureTime) return Task.FromResult((true, typeof(NextStateRouter).Name));
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_contractState.CandidatureTime);
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
        }
    }
}