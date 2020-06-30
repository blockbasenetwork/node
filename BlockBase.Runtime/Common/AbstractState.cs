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
        Task<string> Run(CancellationToken cancellationToken);
    }
    public abstract class AbstractState<TStartState, TEndState> : IState
            where TStartState : IState
            where TEndState : IState
    {
        private bool _verbose;

        protected ILogger _logger;
        protected TimeSpan _delay;

        protected bool _inAutomaticMode;

        protected string Path { get; set; }

        public AbstractState(ILogger logger, bool automatic = false, bool verbose = false)
        {
            _logger = logger;
            _delay = TimeSpan.FromSeconds(5);
            _verbose = verbose;
            _inAutomaticMode = automatic;
            Path = this.GetType().FullName;
        }
        public virtual async Task<string> Run(CancellationToken cancellationToken)
        {

            while (true)
            {
                try
                {
                    
                    //checks if execution is cancelled
                    if (cancellationToken.IsCancellationRequested) 
                    {
                        _logger.LogInformation($"{Path} - Cancellation requested jumping to {typeof(TEndState).Name}");
                        return typeof(TEndState).Name;
                    }

                    await Task.Delay(_delay);
                    //resetting delay every time after it has been done to avoid a situation where a very short delay gets set and no condition resets it to a higher value
                    _delay = TimeSpan.FromSeconds(5);

                    _logger.LogDebug($"{Path} - Starting to update status...");
                    await UpdateStatus();
                    _logger.LogInformation($"{Path} - Status updated");


                    if (!await HasConditionsToContinue())
                    {
                        _logger.LogDebug($"{Path} - No conditions to continue in this state");
                        //if there are no conditions to continue on the start state, jump to the end state
                        if(this is TStartState) return typeof(TEndState).Name;
                        return typeof(TStartState).Name; //returns control to the State Manager indicating same state
                    }

                    var jumpStatus = await HasConditionsToJump();
                    if (jumpStatus.inConditionsToJump)
                    {
                        _logger.LogInformation($"{Path} - Jumping to {jumpStatus.nextState}");
                        return jumpStatus.nextState;
                    }

                    if (!await IsWorkDone())
                    {
                        _logger.LogDebug($"{Path} - Starting to do work...");
                        await DoWork();
                        _logger.LogInformation($"{Path} - Work done");
                    }
                    else
                    {
                        _logger.LogDebug($"{Path} - No work found to do");
                    }
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{Path} | {ex.Message}");
                    _logger.LogDebug($"Trace: {ex}");

                    var crashDelay = TimeSpan.FromSeconds(3);
                    await Task.Delay(crashDelay);
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