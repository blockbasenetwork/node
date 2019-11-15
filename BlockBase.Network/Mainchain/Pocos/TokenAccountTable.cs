using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TokenAccountTable
    {
        [JsonProperty("balance")]
        public string Balance { get; set; }
    }
}
