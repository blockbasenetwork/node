using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    public class NetworkReactionState : AbstractState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private ContractStateTable _contractStateTable;
        private NodeConfigurations _nodeConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private SidechainPool _sidechainPool;

        //TODO rpinto - this state has to be fast in jumping to block validation to not miss the connection
        public NetworkReactionState(ILogger logger, NodeConfigurations nodeConfigurations, IMainchainService mainchainService, SidechainPool sidechainPool) : base(logger)
        {
            _nodeConfigurations = nodeConfigurations;
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _producerList == null) return Task.FromResult(false);
            //verifies if he is a producer and the sidechain is in production state
            return Task.FromResult(_contractStateTable.ProductionTime && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_currentProducer.Producer == _nodeConfigurations.AccountName)
                return Task.FromResult((true, typeof(ProduceBlockState).Name));

            else return Task.FromResult((true, typeof(StartState).Name));
            // else
            //     return Task.FromResult((true, typeof(VoteBlockState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            //there shouldn't be any work to do
            return Task.FromResult(true);
        }

        protected override async Task UpdateStatus()
        {
            //fetches data related to the state of the sidechain, and information about if he needs to produce a block or vote on one

            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if (contractState == null) return;
            if (producerList == null) return;

            _contractStateTable = contractState;
            _producerList = producerList;
            _currentProducer = currentProducer;
        }
    }
}
