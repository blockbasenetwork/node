using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ClientTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("publickey")]
        public string PublicKey { get; set; }
    }
}
