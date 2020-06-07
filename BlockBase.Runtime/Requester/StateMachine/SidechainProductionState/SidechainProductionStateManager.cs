using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.SidechainProductionState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainProductionState
{
    public class SidechainProductionStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        public SidechainProductionStateManager(ILogger logger) : base(logger)
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
