using Newtonsoft.Json;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ContractStateTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("startchain")]
        public bool Startchain { get; set; }

        [JsonProperty("configtime")]
        public bool ConfigTime { get; set; }

        [JsonProperty("candidaturetime")]
        public bool CandidatureTime { get; set; }

        [JsonProperty("secrettime")]
        public bool SecretTime { get; set; }

        [JsonProperty("ipsendtime")]
        public bool IPSendTime { get; set; }

        [JsonProperty("ipreceivetime")]
        public bool IPReceiveTime { get; set; }

        [JsonProperty("productiontime")]
        public bool ProductionTime { get; set; }
    }
}