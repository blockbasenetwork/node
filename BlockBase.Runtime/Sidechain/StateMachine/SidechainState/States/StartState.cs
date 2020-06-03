using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class StartState : AbstractState
    {
        public StartState(CurrentGlobalStatus status, ILogger logger): base(status, logger)
        {
            
        }

        protected override Task<bool> IsWorkDone()
        {
            throw new System.NotImplementedException();
        }

        protected override async Task DoWork()
        {

        }

        protected override async Task<bool> HasConditionsToContinue()
        {
            if(Status.Sidechain.AreCommunicationsDead()) return false;
            //Needs to check other conditions
            return true;
        }
        
        protected override async Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //TODO
            return (false, null);
        }

        protected override async Task UpdateStatus() 
        {
            await Status.Sidechain.TryUpdateSidechainStatus(Status.Local.ClientAccountName);
        }

    }

}