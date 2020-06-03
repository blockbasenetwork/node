using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class IPReceiveState : AbstractState
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        public IPReceiveState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(sidechain, logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_producers.Where(p => p.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.IsReadyToProduce ?? false);
        }

        protected override async Task DoWork()
        {
            await _mainchainService.NotifyReady(Sidechain.ClientAccountName, _nodeConfigurations.AccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((_contractStateTable.IPReceiveTime || _contractStateTable.ProductionTime) && isProducerInTable);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isProducerInTable && _contractStateTable.ProductionTime, typeof(ProductionState).Name));
        }

        protected override async Task UpdateStatus()
        {
            var producers = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);
            var contractState = await _mainchainService.RetrieveContractState(Sidechain.ClientAccountName);
            
            _producers = producers;
            _contractStateTable = contractState;
        }
    }

}