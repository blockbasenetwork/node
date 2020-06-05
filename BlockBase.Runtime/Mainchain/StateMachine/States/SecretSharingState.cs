using System.Threading.Tasks;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.States
{
    public class SecretSharingState : AbstractMainchainState<StartState, EndState>
    {
        public SecretSharingState(ILogger logger) : base(logger)
        {
        }

        protected override Task DoWork()
        {
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
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