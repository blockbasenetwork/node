using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider.StateMachine.HistoryValidation.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.HistoryValidationState
{
    public class HistoryValidationStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private SidechainPool _sidechainPool;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private IMongoDbProducerService _mongoDbProducerService;


        public HistoryValidationStateManager(ILogger logger,
            SidechainPool sidechainPool, NodeConfigurations nodeConfigurations,
            IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;

        }

        protected override IState BuildState(string state)
        {
            if (state == typeof(StartState).Name) return new StartState(_sidechainPool, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(ValidateHistoryState).Name) return new ValidateHistoryState(_logger, _mainchainService, _mongoDbProducerService, _sidechainPool, _nodeConfigurations);
            if (state == typeof(EndState).Name) return new EndState(_logger, _sidechainPool, _mainchainService);
            if (state == typeof(WaitForEndConfirmationState).Name) return new WaitForEndConfirmationState(_sidechainPool, _logger, _mainchainService, _nodeConfigurations);
            throw new System.NotImplementedException();
        }
    }
}
