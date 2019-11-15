using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class PendingRewardTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("reward")]
        public uint Reward { get; set; }
    }
}
