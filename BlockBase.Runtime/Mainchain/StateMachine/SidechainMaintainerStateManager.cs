using BlockBase.Runtime.Common;
using BlockBase.Runtime.Mainchain.StateMachine.States;

namespace BlockBase.Runtime.Mainchain.StateMachine
{
    public class SidechainMaintainerStateManager : AbstractStateManager<StartState, EndState>
    {
        protected override IState BuildState(string state)
        {
            throw new System.NotImplementedException();
        }
    }
}
