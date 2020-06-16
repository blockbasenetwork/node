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
                        _logger.LogDebug($"{Path} - Jumping to {typeof(TEndState).Name}");
                        return typeof(TEndState).Name;
                    }

                    if(_verbose) _logger.LogDebug($"{Path} - Starting to delay... {_delay.Seconds} seconds");
                    await Task.Delay(_delay);
                    //resetting delay every time after it has been done to avoid a situation where a very short delay gets set and no condition resets it to a higher value
                    _delay = TimeSpan.FromSeconds(5);
                    if(_verbose) _logger.LogDebug($"{Path} - Finished delay");

                    if(_verbose) _logger.LogDebug($"{Path} - Starting to update status...");
                    await UpdateStatus();
                    if(_verbose) _logger.LogDebug($"{Path} - Status updated");

                    if(_verbose) _logger.LogDebug($"{Path} - Starting to verify if has conditions to continue...");
                    if (!await HasConditionsToContinue())
                    {
                        if(_verbose) _logger.LogDebug($"{Path} - No conditions to continue in this state");
                        //if there are no conditions to continue on the start state, jump to the end state
                        if(this is TStartState) return typeof(TEndState).Name;
                        return typeof(TStartState).Name; //returns control to the State Manager indicating same state
                    }
                    if(_verbose) _logger.LogDebug($"{Path} - Conditions to continue verified");

                    if(_verbose) _logger.LogDebug($"{Path} - Starting to verify if has conditions to jump...");
                    var jumpStatus = await HasConditionsToJump();
                    if (jumpStatus.inConditionsToJump)
                    {
                        if(_verbose) _logger.LogDebug($"{Path} - Jumping to {jumpStatus.nextState}");
                        return jumpStatus.nextState;
                    }
                    if(_verbose) _logger.LogDebug($"{Path} - No conditions found to jump");

                    if(_verbose) _logger.LogDebug($"{Path} - Starting to verify if has work to be done...");
                    if (!await IsWorkDone())
                    {
                        if(_verbose) _logger.LogDebug($"{Path} - Starting to do work...");
                        await DoWork();
                        _logger.LogDebug($"{Path} - Work done");
                    }
                    else
                    {
                        _logger.LogDebug($"{Path} - No work found to do");
                    }
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{Path} | {ex.Message}");
                    if(_verbose) _logger.LogDebug($"Trace: {ex}");

                    var crashDelay = TimeSpan.FromSeconds(3);
                    if(_verbose) _logger.LogDebug($"{Path} - Starting after crash delay... {crashDelay.Seconds} seconds");
                    await Task.Delay(crashDelay);
                    if(_verbose) _logger.LogDebug($"{Path} - Finished after crash delay");
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