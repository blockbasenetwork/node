using System.Threading.Tasks;
using BlockBase.Runtime.SidechainState;
using BlockBase.Runtime.SidechainState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.BlockProductionState.States
{
    public class CheckContactsState : AbstractState
    {
        public CheckContactsState(CurrentGlobalStatus status, ILogger logger) : base(status, logger)
        {
        }

        protected override Task DoWork()
        {
            //checks comms and updates to whom he should be connected to
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            //if he has no contacts there shouldn't be no condition to continue
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            
            //jumps to the SynchronizeNodeState
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            //there isn't a clear rule to determine if the work is done because he could always improve a little in subsequent runs
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            //fetches contact data
            throw new System.NotImplementedException();
        }
    }
}
