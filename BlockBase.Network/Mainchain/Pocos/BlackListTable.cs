using Newtonsoft.Json;
using BlockBase.Domain.Eos;
namespace BlockBase.Network.Mainchain.Pocos
{
    public class BlackListTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }
    }
}