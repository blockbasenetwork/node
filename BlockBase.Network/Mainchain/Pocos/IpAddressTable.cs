using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class IPAddressTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("publickey")]
        public string PublicKey { get; set; }

        [JsonProperty("encryptedips")]
        public List<string> EncryptedIPs { get; set; }
    }
}
