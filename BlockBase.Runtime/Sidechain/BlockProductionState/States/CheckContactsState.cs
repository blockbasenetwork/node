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
            
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            
            //jumps to the SynchronizeNodeState
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            
            throw new System.NotImplementedException();
        }
    }
}
