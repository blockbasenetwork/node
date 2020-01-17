using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TokenAccountTable
    {
        [JsonProperty(EosAtributeNames.BALANCE)]
        public string Balance { get; set; }
    }
}
