using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Common
{
    public abstract class ProviderAbstractState<TStartState, TEndState, TWaitForEndConfirmationState> : AbstractState<TStartState, TEndState, TWaitForEndConfirmationState>
            where TStartState : IState
            where TEndState : IState
            where TWaitForEndConfirmationState : IState
    {

        protected SidechainPool _sidechainPool;
        protected IMainchainService _mainchainService;
        protected ProviderAbstractState(ILogger logger, SidechainPool sidechainPool, IMainchainService mainchainService, bool verbose = false) : base(logger, verbose)
        {
            Path = $"{sidechainPool.ClientAccountName} - {this.GetType().FullName}";
            _sidechainPool = sidechainPool;
            _mainchainService = mainchainService;
        }

        public override async Task<string> Run(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"{Path} - Checking if sidechain is still the same...");
            if (!await IsSidechainStillTheSame())
            {

                _logger.LogDebug($"{Path} - Sidechain is no longer the same.");
                //if there are no conditions to continue on the start state, jump to the end state
                if (this is TStartState) return typeof(TEndState).Name;
                return typeof(TStartState).Name; //returns control to the State Manager indicating same state
            }
            _logger.LogInformation($"{Path} - Sidechain checked");
            return await base.Run(cancellationToken);

        }


        protected async Task<bool> IsSidechainStillTheSame()
        {
            var clientTable = await _mainchainService.RetrieveClientTable(_sidechainPool.ClientAccountName);
            return _sidechainPool.SidechainCreationTimestamp == clientTable.SidechainCreationTimestamp;
        }


    }
}