using System.Threading.Tasks;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionState.States
{
    public class EndState : AbstractState<StartState, EndState>
    {

        public EndState(ILogger logger): base(logger)
        {
            
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(true);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((true, string.Empty));
        }

        protected override Task UpdateStatus() 
        {
            
            return Task.CompletedTask;
        }

    }

}