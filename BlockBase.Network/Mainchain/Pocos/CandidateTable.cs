using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class CandidateTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("publickey")]
        public string PublicKey { get; set; }

        [JsonProperty("stake")]
        public uint Stake { get; set; }

        [JsonProperty("worktimeinseconds")]
        public uint WorkTimeInSeconds { get; set; }
    }    
}
