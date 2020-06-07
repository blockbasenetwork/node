using BlockBase.Network.Sidechain;
using BlockBase.Runtime.StateMachine.SidechainState;
using System.Collections.Concurrent;

namespace BlockBase.Runtime.Provider
{
    public class SidechainContext
    {
        public SidechainStateManager SidechainStateManager { get; set; }
        public SidechainPool SidechainPool { get; set; }
    }
}