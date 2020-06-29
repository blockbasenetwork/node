using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Requester.StateMachine.SidechainProductionState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainProductionState
{
    public class SidechainProductionStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private TransactionsManager _transactionsManager;
        public SidechainProductionStateManager(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, TransactionsManager transactionsManager) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _transactionsManager = transactionsManager;
        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(NextStateRouter).Name) return new NextStateRouter(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(SwitchProducerTurn).Name) return new SwitchProducerTurn(_logger,_mainchainService, _nodeConfigurations, _transactionsManager);
            if(state == typeof(UpdateAuthorizationsState).Name) return new UpdateAuthorizationsState(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(EndState).Name) return new EndState(_logger);
            throw new System.NotImplementedException();
        }
    }
}
