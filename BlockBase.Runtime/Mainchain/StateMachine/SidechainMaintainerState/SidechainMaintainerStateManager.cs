using BlockBase.Runtime.Common;
using BlockBase.Runtime.Mainchain.StateMachine.SidechainMaintainerState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.SidechainMaintainerState.States
{
    public class SidechainMaintainerStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        public SidechainMaintainerStateManager(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_logger);
            if(state == typeof(EndState).Name) return new EndState(_logger);
            throw new System.NotImplementedException();
        }
    }
}
