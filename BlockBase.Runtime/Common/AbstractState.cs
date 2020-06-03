using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Common
{
    public interface IState
    {
        Task<string> Run(CancellationToken cancellationToken = default(CancellationToken));
    }
    public abstract class AbstractState<TStartState, TEndState> : IState
            where TStartState : IState
            where TEndState : IState
    {
        protected ILogger _logger;
        protected int _delay;

        public AbstractState(ILogger logger)
        {
            _logger = logger;
        }
        public virtual async Task<string> Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                try
                {
                    //checks if execution is cancelled
                    if (cancellationToken.IsCancellationRequested) return typeof(TEndState).Name;

                    await UpdateStatus();
                    if (await IsWorkDone()) await Task.Delay(_delay);

                    if (!await HasConditionsToContinue()) return typeof(TStartState).Name; //returns control to the State Manager indicating same state

                    var jumpStatus = await HasConditionsToJump();
                    if (jumpStatus.inConditionsToJump) return jumpStatus.nextState;

                    if (!await IsWorkDone()) await DoWork();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{this.GetType().Name} crashed", ex.Message);
                }
            }
        }


        protected abstract Task UpdateStatus();

        protected abstract Task<bool> HasConditionsToContinue();

        protected abstract Task<bool> IsWorkDone();

        /// <summary>It is very important that DoWork is idempotent!</summary>
        protected abstract Task DoWork();

        protected abstract Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump();

    }
}