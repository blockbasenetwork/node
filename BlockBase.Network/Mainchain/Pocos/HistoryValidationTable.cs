using BlockBase.Domain.Eos;
using Newtonsoft.Json;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class HistoryValidationTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_HASH)]
        public string BlockHash { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_BYTE_IN_HEXADECIMAL)]
        public string BlockByteInHexadecimal { get; set; }
    }
}