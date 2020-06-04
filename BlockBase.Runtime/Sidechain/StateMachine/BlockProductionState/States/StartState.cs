using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.StateMachine.SidechainState;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using Microsoft.Extensions.Logging;
using System.Linq;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Domain.Enums;

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    public class StartState : AbstractState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producerList;
        private SidechainPool _sidechainPool;

        private NodeConfigurations _nodeConfigurations;

        public StartState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, SidechainPool sidechainPool) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
        }

        protected override Task DoWork()
        {
            //nothing to do
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            return Task.FromResult(_contractStateTable.ProductionTime && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //TODO verifies if he is a producer and the sidechain is in production state - should be the same as above
            var inConditionsToJump = _contractStateTable.ProductionTime && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName);
            if(_sidechainPool.ProducerType == ProducerTypeEnum.Validator)
                return Task.FromResult((inConditionsToJump, typeof(SynchronizeValidatorNodeState).Name));
            else return Task.FromResult((inConditionsToJump, typeof(SynchronizeNodeState).Name));
            
        }

        protected override Task<bool> IsWorkDone()
        {
            //nothing to do
            return Task.FromResult(true);
        }

        protected override async Task UpdateStatus()
        {
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);

            _contractStateTable = contractState;
            _producerList = producerList;
            
        }
    }
}
