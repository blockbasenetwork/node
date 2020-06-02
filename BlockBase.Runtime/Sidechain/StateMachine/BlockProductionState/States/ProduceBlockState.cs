using System.Threading.Tasks;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.StateMachine.SidechainState;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    public class ProduceBlockState : AbstractState<StartState, EndState>
    {
        public ProduceBlockState(ILogger logger) : base(logger)
        {
        }

        protected override Task DoWork()
        {
            //produces a block and sends it to the network
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            //and verifies if it's he's turn to produce
            //if he has no contacts there shouldn't be no condition to continue
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //verifies if block has been produced and is in the network
            //verifies if his time to produce is done
            //jumps to the StartState
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            //verifies if block has been produced and is in the network
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            //I don't think he needs to fetch anything
            throw new System.NotImplementedException();
        }
    }
}
