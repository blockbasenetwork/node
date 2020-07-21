using System.Threading.Tasks;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class EndState : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        public EndState(ILogger logger) : base(logger)
        {
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(false);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((false, string.Empty));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task UpdateStatus()
        {
            return Task.CompletedTask;
        }
    }
}