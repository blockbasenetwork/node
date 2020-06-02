using System.Threading.Tasks;
using BlockBase.Runtime.SidechainState;
using BlockBase.Runtime.SidechainState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.BlockProductionState.States
{
    public class StartState : AbstractState
    {
        public StartState(CurrentGlobalStatus status, ILogger logger) : base(status, logger)
        {
        }

        protected override Task DoWork()
        {
            //TODO not really sure if there's anything to do
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //TODO verifies if he is a producer and the sidechain is in production state - should be the same as above
            //jumps to the CheckContactsState
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            //TODO get's data about his state as producer and the state of production of the sidechain
            throw new System.NotImplementedException();
        }
    }
}
