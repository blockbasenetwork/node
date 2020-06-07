using System;
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
    public class ProvidersConnectionState : AbstractMainchainState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractState;
        private ContractInformationTable _contractInfo;

        private bool _verifyBlockPermissionSet;
        private bool _historyValidatePermissionSet;
        public ProvidersConnectionState(ILogger logger, IMainchainService mainchainService, SidechainPool sidechainPool) : base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
        }

        protected async override Task DoWork()
        {
            try
            {
                await _mainchainService.LinkAuthorization(EosMsigConstants.VERIFY_BLOCK_PERMISSION, _sidechainPool.ClientAccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
            }
            catch
            {
                //TODO rpinto - is there a better way to do this than doing it in a catch?
                _verifyBlockPermissionSet = true;
                _logger.LogDebug($"Already linked authorization {EosMsigConstants.VERIFY_BLOCK_PERMISSION}");
            }
            try
            {
                await _mainchainService.LinkAuthorization(EosMethodNames.HISTORY_VALIDATE, _sidechainPool.ClientAccountName, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
            }
            catch
            {
                //TODO rpinto - is there a better way to do this than doing it in a catch?
                _historyValidatePermissionSet = true;
                _logger.LogDebug($"Already linked authorization {EosMethodNames.HISTORY_VALIDATE}");
            }

            if(_verifyBlockPermissionSet && _historyValidatePermissionSet)
            {
                await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.PRODUCTION_TIME, _sidechainPool.ClientAccountName);
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.ReceiveEndDate, 0));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(_contractState.IPReceiveTime) return Task.FromResult((true, typeof(NextStateRouter).Name));
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_contractState.IPReceiveTime);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
        }
    }
}