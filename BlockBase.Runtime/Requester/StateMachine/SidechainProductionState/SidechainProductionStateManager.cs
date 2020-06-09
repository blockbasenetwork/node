using BlockBase.DataPersistence.ProducerData;
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
        private TransactionsHandler _transactionsHandler;
        private IMongoDbProducerService _mongoDbProducerService;
        public SidechainProductionStateManager(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, TransactionsHandler transactionsHandler, IMongoDbProducerService mongoDbProducerService) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _transactionsHandler = transactionsHandler;
            _mongoDbProducerService = mongoDbProducerService;
        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(NextStateRouter).Name) return new NextStateRouter(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(SwitchProducerTurn).Name) return new SwitchProducerTurn(_logger,_mainchainService, _nodeConfigurations, _transactionsHandler, _mongoDbProducerService);
            if(state == typeof(UpdateAuthorizationsState).Name) return new UpdateAuthorizationsState(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(EndState).Name) return new EndState(_logger);
            throw new System.NotImplementedException();
        }
    }
}