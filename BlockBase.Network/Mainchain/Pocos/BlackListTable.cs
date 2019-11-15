using BlockBase.Domain.Blockchain;
using BlockBase.Utils.Crypto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class BlackListTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}