using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ProducerInTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("publickey")]
        public string PublicKey { get; set; }

        [JsonProperty("producerstake")]
        public uint ProducerStake { get; set; }

        [JsonProperty("miningposition")]
        public uint MiningPosition { get; set; }

        [JsonProperty("warning")]
        public uint Warning { get; set; }

        [JsonProperty("worktimeinseconds")]
        public uint WorkTimeInSeconds { get; set; }

        [JsonProperty("startinsidechaindate")]
        public uint StartInSidechainDate { get; set; }

    }
}
