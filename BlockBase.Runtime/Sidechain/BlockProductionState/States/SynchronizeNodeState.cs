using System.Threading.Tasks;
using BlockBase.Runtime.SidechainState;
using BlockBase.Runtime.SidechainState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.BlockProductionState.States
{
    public class SynchronizeNodeState : AbstractState
    {
        public SynchronizeNodeState(CurrentGlobalStatus status, ILogger logger) : base(status, logger)
        {
        }

        protected override Task DoWork()
        {
            //synchronizes the node - it may abort synchronization if it fails to receive blocks for too long
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            //also verifies if he is unable to synchronize the node due to unresponsive nodes
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //TODO verifies if he is synchronized
            //jumps to one of two states, produce block or vote block
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            //TODO verifies if he is synchronized
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            //fetches latest block data
            throw new System.NotImplementedException();
        }
    }
}