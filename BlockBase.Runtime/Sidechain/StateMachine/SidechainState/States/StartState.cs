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

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class StartState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private List<CandidateTable> _candidates;

        private SidechainPool _sidechainPool;
        public StartState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations): base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
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
            return Task.FromResult(_contractStateTable?.Startchain ?? false);
        }
        
        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var isCandidateInTable = _candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

            if (!isProducerInTable && !isCandidateInTable && _contractStateTable.CandidatureTime) return Task.FromResult((true, typeof(CandidatureState).Name));
            if (isCandidateInTable && _contractStateTable.SecretTime) return Task.FromResult((true, typeof(SecretTimeState).Name));
            if (isProducerInTable && _contractStateTable.IPSendTime) return Task.FromResult((true, typeof(IPSendTimeState).Name));
            if (isProducerInTable && _contractStateTable.ProductionTime) return Task.FromResult((true, typeof(ProductionState).Name));
            
            return Task.FromResult((!isProducerInTable && !isCandidateInTable && !_contractStateTable.CandidatureTime, typeof(EndState).Name));
        }

        protected override async Task UpdateStatus() 
        {
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var candidates = await _mainchainService.RetrieveCandidates(_sidechainPool.ClientAccountName);
            var producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);

            _contractStateTable = contractState;
            _producers = producers;
            _candidates = candidates;
            _delay = TimeSpan.FromSeconds(0);
        }

    }

}