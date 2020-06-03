using BlockBase.Runtime.Common;
using BlockBase.Runtime.Mainchain.StateMachine.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine
{
    public class SidechainMaintainerStateManager : AbstractStateManager<StartState, EndState>
    {
        public SidechainMaintainerStateManager(ILogger logger) : base(logger)
        {
        }

        protected override IState BuildState(string state)
        {
            throw new System.NotImplementedException();
        }
    }
}
