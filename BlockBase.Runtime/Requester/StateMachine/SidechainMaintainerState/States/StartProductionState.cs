using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Requester.StateMachine.Common;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class StartProductionState : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractState;
        private ContractInformationTable _contractInfo;
        public StartProductionState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected async override Task DoWork()
        {
            await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.PRODUCTION_TIME, _nodeConfigurations.AccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractState == null || _contractInfo == null) return Task.FromResult(false);
            if(_contractState.CandidatureTime || !IsTimeUpForSidechainPhase(_contractInfo.ReceiveEndDate, 0))
                return Task.FromResult(false);
            return Task.FromResult(true);
            //return Task.FromResult(!IsTimeUpForSidechainPhase(_contractInfo.ReceiveEndDate, 0));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(!_contractState.IPReceiveTime && _contractState.ProductionTime) return Task.FromResult((true, typeof(NextStateRouter).Name));
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(!_contractState.IPReceiveTime && _contractState.ProductionTime);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);
        }
    }
}