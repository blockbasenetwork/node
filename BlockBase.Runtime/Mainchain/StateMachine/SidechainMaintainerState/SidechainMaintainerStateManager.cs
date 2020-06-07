using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Mainchain.StateMachine.SidechainMaintainerState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.SidechainMaintainerState
{
    public class SidechainMaintainerStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        
        public SidechainMaintainerStateManager(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(EndState).Name) return new EndState(_logger);
            throw new System.NotImplementedException();
        }
    }
}
