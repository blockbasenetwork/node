using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class WaitForEndConfirmationState : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private DateTime _waitingStartDate;

        public WaitForEndConfirmationState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _waitingStartDate = DateTime.UtcNow;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task DoWork()
        {
            return default(Task);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_waitingStartDate > DateTime.UtcNow.AddDays(-1));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_contractStateTable != null, typeof(StartState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            if (_contractStateTable == null) _mainchainService.ChangeNetwork();

            _delay = TimeSpan.FromSeconds(10);
        }

    }

}