using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TokenLedgerTable
    {
        [JsonProperty(EosAtributeNames.SIDECHAIN)]
        public string Sidechain { get; set; }
        [JsonProperty(EosAtributeNames.OWNER)]
        public string Owner { get; set; }
        [JsonProperty(EosAtributeNames.STAKE)]
        public string Stake { get; set; }
    }
}
