using BlockBase.Network.Sidechain;

namespace BlockBase.Runtime.SidechainState
{
    public class CurrentGlobalStatus
    {
        public CurrentSidechainStatus Sidechain { get; set; }
        public SidechainPool Local { get; set; }

    }
}