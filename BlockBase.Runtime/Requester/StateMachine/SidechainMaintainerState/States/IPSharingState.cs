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
    public class IPSharingState : AbstractMainchainState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractState;
        private ContractInformationTable _contractInfo;

        public IPSharingState(ILogger logger, IMainchainService mainchainService, SidechainPool sidechainPool) : base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
        }

        protected override async Task DoWork()
        {
            await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SEND_TIME, _sidechainPool.ClientAccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.SendEndDate, 0));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(_contractState.IPSendTime) return Task.FromResult((true, typeof(UpdateAuthorizationsState).Name));
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_contractState.IPSendTime);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
        }
    }
}