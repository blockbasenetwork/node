using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState
{
    public class SidechainMaintainerStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private RequesterConfigurations _requesterConfigurations;
        private IMongoDbRequesterService _mongoDbRequesterService;



        public SidechainMaintainerStateManager(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IMongoDbRequesterService mongoDbRequesterService, RequesterConfigurations requesterConfigurations) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbRequesterService = mongoDbRequesterService;
            _requesterConfigurations = requesterConfigurations;
        }

        protected override IState BuildState(string state)
        {
            if (state == typeof(StartState).Name) return new StartState(_logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(CandidatureReceivalState).Name) return new CandidatureReceivalState(_logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(SecretSharingState).Name) return new SecretSharingState(_logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(IPSharingState).Name) return new IPSharingState(_logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(ProvidersConnectionState).Name) return new ProvidersConnectionState(_logger, _mainchainService, _nodeConfigurations);

            if (state == typeof(NextStateRouter).Name) return new NextStateRouter(_logger, _mainchainService, _nodeConfigurations, _mongoDbRequesterService, _requesterConfigurations);
            if (state == typeof(UpdateAuthorizationsState).Name) return new UpdateAuthorizationsState(_logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(AutomaticConfigUpdateState).Name) return new AutomaticConfigUpdateState(_logger, _mainchainService, _nodeConfigurations, _mongoDbRequesterService);
            if (state == typeof(StartProductionState).Name) return new StartProductionState(_logger, _mainchainService, _nodeConfigurations);

            if (state == typeof(EndState).Name) return new EndState(_logger);
            if (state == typeof(WaitForEndConfirmationState).Name) return new WaitForEndConfirmationState(_logger, _mainchainService, _nodeConfigurations);
            throw new System.NotImplementedException();
        }
    }
}
