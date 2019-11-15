using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TokenLedgerTable
    {
        [JsonProperty("sidechain")]
        public string Sidechain { get; set; }
        [JsonProperty("owner")]
        public string Owner { get; set; }
        [JsonProperty("stake")]
        public string Stake { get; set; }
    }
}
