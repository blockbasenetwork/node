using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Provider.StateMachine.SidechainState;

namespace BlockBase.Runtime.Provider
{
    public class SidechainContext
    {
        public SidechainStateManager SidechainStateManager { get; set; }
        public SidechainPool SidechainPool { get; set; }
    }
}