using System.Threading.Tasks;
using BlockBase.Network.Sidechain;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class EndState : AbstractState
    {
        public EndState(SidechainPool sidechain, ILogger logger) : base(sidechain, logger)
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