using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Common
{
    public abstract class AbstractStateManager<TStartState, TEndState> : IThreadableComponent
            where TStartState : IState
            where TEndState : IState
    {
        public TaskContainer TaskContainer { get; private set; }

        public TaskContainer Start()
        {
            if(TaskContainer != null) TaskContainer.Stop();
            TaskContainer = TaskContainer.Create(async () => await Run());
            TaskContainer.Start();
            return TaskContainer;
        }

        private async Task Run() 
        {
            var currentState = BuildState(typeof(StartState).Name);


            while(true)
            {
                var nextStateName = await currentState.Run();
                currentState = BuildState(nextStateName);

                if(currentState.GetType() == typeof(EndState))
                {
                    await currentState.Run();
                    break;
                }
            }
        }

        protected abstract IState BuildState(string state);
    }
}
