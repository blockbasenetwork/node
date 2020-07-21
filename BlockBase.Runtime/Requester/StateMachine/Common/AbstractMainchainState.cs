using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.Common
{
    public abstract class AbstractMainchainState<TStartState, TEndState, TWaitForEndConfirmationState> : AbstractState<TStartState, TEndState, TWaitForEndConfirmationState>
            where TStartState : IState
            where TEndState : IState
            where TWaitForEndConfirmationState : IState
    {

        public AbstractMainchainState(ILogger logger, bool verbose = false) : base(logger, verbose)
        {
        }

        protected virtual bool IsTimeUpForSidechainPhase(long sidechainPhaseEndDate, int deltaToExecuteTransactions)
        {
            return sidechainPhaseEndDate * 1000 - deltaToExecuteTransactions <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        protected virtual bool IsTimeUpForProducer(CurrentProducerTable currentProducer, ContractInformationTable contractInfo)
        {
            return (currentProducer.StartProductionTime + contractInfo.BlockTimeDuration) * 1000 <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}