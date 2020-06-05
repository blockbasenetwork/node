using System.Threading.Tasks;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.States
{
    public class CandidatureReceivalState : AbstractMainchainState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private ContractInformationTable _contractInfo;
        private SidechainPool _sidechainPool;

        private ContractStateTable _contractState;
        public CandidatureReceivalState(ILogger logger, IMainchainService mainchainService, SidechainPool sidechainPool) : base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
        }

        protected override async Task DoWork()
        {
            await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_CANDIDATURE_TIME, _sidechainPool.ClientAccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.CandidatureEndDate, 0));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(_contractState.CandidatureTime) return Task.FromResult((true, typeof(UpdateAuthorizationsState).Name));
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_contractState.CandidatureTime);
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
        }
    }
}