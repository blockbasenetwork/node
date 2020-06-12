using System.Threading;
using System.Threading.Tasks;
using BlockBase.Network.Sidechain;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Common
{
    public abstract class ProviderAbstractState<TStartState, TEndState> : AbstractState<TStartState, TEndState>
            where TStartState : IState
            where TEndState : IState
    {
        protected ProviderAbstractState(ILogger logger, SidechainPool sidechainPool, bool automatic = false, bool verbose = false) : base(logger, automatic, verbose)
        {
            Path = $"{sidechainPool.ClientAccountName} - {this.GetType().FullName}";
        }
    }
}