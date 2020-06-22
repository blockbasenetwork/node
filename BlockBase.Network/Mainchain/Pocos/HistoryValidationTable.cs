using System.Collections.Generic;
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

        [JsonProperty(EosAtributeNames.VERIFY_SIGNATURES)]
        public List<string> VerifySignatures {get; set;}

        [JsonProperty(EosAtributeNames.PACKED_TRANSACTION)]
        public string ValidateHistoryPackedTransaction { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_BYTE_IN_HEXADECIMAL)]
        public string BlockByteInHexadecimal { get; set; }
    }
}