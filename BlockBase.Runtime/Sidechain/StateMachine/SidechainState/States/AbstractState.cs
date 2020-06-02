using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public abstract class AbstractState
    {
        protected ILogger _logger;
        protected CurrentGlobalStatus Status { get; private set; }

        protected bool IsWorkFinished { get; set; }

        public AbstractState(CurrentGlobalStatus status, ILogger logger)
        {
            Status = status;
            _logger = logger;
        }
        public virtual async Task<string> Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            while(true)
            {
                try
                {
                    //checks if execution is cancelled
                    if(cancellationToken.IsCancellationRequested) return typeof(EndState).Name;


                    await UpdateStatus();
                    if(!await HasConditionsToContinue()) return typeof(StartState).Name; //returns control to the State Manager indicating same state
                    
                    var jumpStatus = await HasConditionsToJump();
                    if(jumpStatus.inConditionsToJump) return jumpStatus.nextState;

                    if (!await IsWorkDone()) await DoWork();
                }
                catch(Exception ex)
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