using System;
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
    public class ProvidersConnectionState : AbstractMainchainState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractState;

        private bool _verifyBlockPermissionSet;
        private bool _historyValidatePermissionSet;
        public ProvidersConnectionState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected async override Task DoWork()
        {
            try
            {
                await _mainchainService.LinkAuthorization(EosMsigConstants.VERIFY_BLOCK_PERMISSION, _nodeConfigurations.AccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
            }
            catch
            {
                //TODO rpinto - is there a better way to do this than doing it in a catch?
                _verifyBlockPermissionSet = true;
                _logger.LogDebug($"Already linked authorization {EosMsigConstants.VERIFY_BLOCK_PERMISSION}");
            }
            try
            {
                await _mainchainService.LinkAuthorization(EosMethodNames.HISTORY_VALIDATE, _nodeConfigurations.AccountName, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
            }
            catch
            {
                //TODO rpinto - is there a better way to do this than doing it in a catch?
                _historyValidatePermissionSet = true;
                _logger.LogDebug($"Already linked authorization {EosMethodNames.HISTORY_VALIDATE}");
            }

            if(_verifyBlockPermissionSet && _historyValidatePermissionSet)
            {
                await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.PRODUCTION_TIME, _nodeConfigurations.AccountName);
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractState != null);
            //return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.ReceiveEndDate, 0));
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
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
        }
    }
}