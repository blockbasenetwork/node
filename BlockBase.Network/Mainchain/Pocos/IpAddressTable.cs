using Newtonsoft.Json;
using System.Collections.Generic;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class IPAddressTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.PUBLIC_KEY)]
        public string PublicKey { get; set; }

        [JsonProperty(EosAtributeNames.ENCRYPTED_IPS)]
        public List<string> EncryptedIPs { get; set; }
    }
}
