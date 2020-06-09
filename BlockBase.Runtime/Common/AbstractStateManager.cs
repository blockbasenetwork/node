using System;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Common
{

    public abstract class AbstractStateManager<TStartState, TEndState> : IThreadableComponent
            where TStartState : IState
            where TEndState : IState
    {


        private ILogger _logger;

        public TaskContainer TaskContainer { get; private set; }

        public virtual TaskContainer Start()
        {
            if (TaskContainer != null) TaskContainer.Stop();
            TaskContainer = TaskContainer.Create(async () => await Run());
            TaskContainer.Start();
            return TaskContainer;
        }

        public virtual void Stop()
        {
            if (TaskContainer != null) TaskContainer.Stop();
        }

        public AbstractStateManager(ILogger logger)
        {
            _logger = logger;
        }

        protected virtual async Task Run()
        {
            var nextStateName = typeof(StartState).Name;

            while (true)
            {
                try
                {
                    var currentState = BuildState(nextStateName);
                    nextStateName = await currentState.Run(TaskContainer.CancellationTokenSource.Token);

                    if (currentState.GetType() == typeof(EndState))
                    {
                        await currentState.Run();
                        //TODO rpinto - is this dangerous? should the stop mechanism be delegated to an upper level?
                        this.Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{this.GetType().Name} crashed {ex.Message}");
                    _logger.LogDebug($"Trace: {ex}");

                    var crashDelay = TimeSpan.FromSeconds(10);
                    _logger.LogDebug($"{this.GetType().Name} - Starting after crash delay... {crashDelay.Seconds} seconds");
                    await Task.Delay(crashDelay);
                    _logger.LogDebug($"{this.GetType().Name} - Finished after crash delay");
                }
            }
        }

        protected abstract IState BuildState(string state);
    }
}
