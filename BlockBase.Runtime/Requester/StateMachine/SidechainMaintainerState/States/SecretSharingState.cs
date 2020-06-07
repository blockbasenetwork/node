using System.Threading.Tasks;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class SecretSharingState : AbstractMainchainState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractState;

        private ContractInformationTable _contractInfo;
        public SecretSharingState(ILogger logger, IMainchainService mainchainService, SidechainPool sidechainPool) : base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
            
        }

        protected override async Task DoWork()
        {
            await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SECRET_TIME, _sidechainPool.ClientAccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.SecretEndDate, 0));
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
            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
        }
    }
}