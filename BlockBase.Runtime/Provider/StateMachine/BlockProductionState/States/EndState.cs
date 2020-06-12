using System.Threading.Tasks;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.BlockProductionState.States
{
    public class EndState : ProviderAbstractState<StartState, EndState>
    {
        public EndState(ILogger logger, SidechainPool sidechainPool) : base(logger, sidechainPool)
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
            return Task.FromResult((true, string.Empty));
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