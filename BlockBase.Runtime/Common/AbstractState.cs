using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        protected TimeSpan _delay;

        public AbstractState(ILogger logger)
        {
            _logger = logger;
            _delay = TimeSpan.FromSeconds(2);
        }
        public virtual async Task<string> Run(CancellationToken cancellationToken = default(CancellationToken))
        {

            while (true)
            {
                try
                {
                    //checks if execution is cancelled
                    if (cancellationToken.IsCancellationRequested) return typeof(TEndState).Name;

                    _logger.LogDebug($"{this.GetType().Name} - Starting to update status...");
                    await UpdateStatus();
                    _logger.LogDebug($"{this.GetType().Name} - Status updated");

                    _logger.LogDebug($"{this.GetType().Name} - Starting to verify if has conditions to continue...");
                    if (!await HasConditionsToContinue())
                    {
                        _logger.LogDebug($"{this.GetType().Name} - No conditions to continue in this state");
                        return typeof(TStartState).Name; //returns control to the State Manager indicating same state
                    }
                    _logger.LogDebug($"{this.GetType().Name} - Conditions to continue verified");

                    _logger.LogDebug($"{this.GetType().Name} - Starting to verify if has conditions to jump...");
                    var jumpStatus = await HasConditionsToJump();
                    if (jumpStatus.inConditionsToJump)
                    {
                        _logger.LogDebug($"{this.GetType().Name} - Conditions found to jump to {jumpStatus.nextState}");
                        return jumpStatus.nextState;
                    }
                    _logger.LogDebug($"{this.GetType().Name} - No conditions found to jump");

                    _logger.LogDebug($"{this.GetType().Name} - Starting to verify if has work to be done...");
                    if (!await IsWorkDone())
                    {
                        _logger.LogDebug($"{this.GetType().Name} - Starting to do work...");
                        await DoWork();
                        _logger.LogDebug($"{this.GetType().Name} - Work done");
                    }
                    else
                    {
                        _logger.LogDebug($"{this.GetType().Name} - No work found to do");
                    }
                    _logger.LogDebug($"{this.GetType().Name} - Starting to delay... {_delay.Seconds} seconds");
                    await Task.Delay(_delay);
                    _logger.LogDebug($"{this.GetType().Name} - Finished delay");
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




        protected abstract Task UpdateStatus();

        protected abstract Task<bool> HasConditionsToContinue();

        protected abstract Task<bool> IsWorkDone();

        /// <summary>It is very important that DoWork is idempotent!</summary>
        protected abstract Task DoWork();

        protected abstract Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump();

    }
}