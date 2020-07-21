using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.BlockProductionState.States
{
    public class WaitForEndConfirmationState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private ClientTable _clientTable;
        private List<ProducerInTable> _producers;
        private DateTime _waitingStartDate;

        public WaitForEndConfirmationState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
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
            if (_clientTable != null) return Task.FromResult(_clientTable.SidechainCreationTimestamp == _sidechainPool.SidechainCreationTimestamp);
            return Task.FromResult(_waitingStartDate > DateTime.UtcNow.AddMinutes(-30));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_contractStateTable != null && _producers != null && _clientTable?.SidechainCreationTimestamp == _sidechainPool.SidechainCreationTimestamp, typeof(StartState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _clientTable = await _mainchainService.RetrieveClientTable(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            if (_contractStateTable == null || _producers == null) _mainchainService.ChangeNetwork();

            _delay = TimeSpan.FromSeconds(10);
        }

    }

}