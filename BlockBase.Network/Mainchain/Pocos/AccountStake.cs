using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class AccountStake
    {
        public string Sidechain { get; set; }
        public string Owner { get; set; }
        public string StakeString { get; set; }
        public decimal Stake { get; set; }
    }
}
