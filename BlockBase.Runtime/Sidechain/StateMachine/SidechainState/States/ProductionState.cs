using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class ProductionState : AbstractState
    {
        public ProductionState(CurrentGlobalStatus status, ILogger logger) : base(status, logger)
        {

        }

        protected override Task<bool> IsWorkDone()
        {
            throw new System.NotImplementedException();
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

        protected override Task UpdateStatus()
        {
            throw new System.NotImplementedException();
        }
    }

}