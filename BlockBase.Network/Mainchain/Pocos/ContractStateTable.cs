using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ContractStateTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.HAS_CHAIN_STARTED)]
        public bool Startchain { get; set; }

        [JsonProperty(EosAtributeNames.IS_CONFIGURATION_PHASE)]
        public bool ConfigTime { get; set; }

        [JsonProperty(EosAtributeNames.IS_CANDIDATURE_PHASE)]
        public bool CandidatureTime { get; set; }

        [JsonProperty(EosAtributeNames.IS_SECRET_SENDING_PHASE)]
        public bool SecretTime { get; set; }

        [JsonProperty(EosAtributeNames.IS_IP_SENDING_PHASE)]
        public bool IPSendTime { get; set; }

        [JsonProperty(EosAtributeNames.IS_IP_RETRIEVING_PHASE)]
        public bool IPReceiveTime { get; set; }

        [JsonProperty(EosAtributeNames.IS_PRODUCTION_PHASE)]
        public bool ProductionTime { get; set; }
    }
}