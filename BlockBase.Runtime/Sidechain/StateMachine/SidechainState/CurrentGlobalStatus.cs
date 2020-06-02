using BlockBase.Network.Sidechain;

namespace BlockBase.Runtime.StateMachine.SidechainState
{
    public class CurrentGlobalStatus
    {
        public CurrentSidechainStatus Sidechain { get; set; }
        public SidechainPool Local { get; set; }

    }
}