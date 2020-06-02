using System.Threading.Tasks;
using BlockBase.Runtime.SidechainState;
using BlockBase.Runtime.SidechainState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.BlockProductionState.States
{
    public class VoteBlockState : AbstractState
    {
        public VoteBlockState(CurrentGlobalStatus status, ILogger logger) : base(status, logger)
        {
        }

        protected override Task DoWork()
        {
            //verifies a block and votes on it if ok
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            //and verifies if it's time for him to vote
            //if he has no contacts there shouldn't be no condition to continue
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //verifies if block is valid and if in that case he's vote is in the network
            //verifies if his time to vote is done
            //jumps to the StartState
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            //verifies if block is valid and if in that case he's vote is in the network
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            //I don't think he needs to fetch anything
            throw new System.NotImplementedException();
        }
    }
}
