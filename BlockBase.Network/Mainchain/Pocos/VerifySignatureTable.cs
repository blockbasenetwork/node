using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class VerifySignatureTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_HASH)]
        public string BlockHash { get; set; }

        [JsonProperty(EosAtributeNames.VERIFY_SIGNATURE)]
        public string VerifySignature { get; set; }

        [JsonProperty(EosAtributeNames.PACKED_TRANSACTION)]
        public string PackedTransaction { get; set; }
    }
}