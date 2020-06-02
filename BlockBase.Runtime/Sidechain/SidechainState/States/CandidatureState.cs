using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.SidechainState.States
{
    public class CandidatureState : AbstractState
    {
        public CandidatureState(CurrentGlobalStatus status, ILogger logger) : base(status, logger)
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

        protected override Task UpdateStatus()
        {
            throw new System.NotImplementedException();
        }
    }

}