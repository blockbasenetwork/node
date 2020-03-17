using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ProducerInTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.PUBLIC_KEY)]
        public string PublicKey { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER_STAKE)]
        public uint ProducerStake { get; set; }

        [JsonProperty(EosAtributeNames.WARNING_TYPE)]
        public uint Warning { get; set; }

        [JsonProperty(EosAtributeNames.WORK_DURATION_IN_SECONDS)]
        public uint WorkTimeInSeconds { get; set; }

        [JsonProperty(EosAtributeNames.SIDECHAIN_START_DATE_IN_SECONDS)]
        public uint StartInSidechainDate { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER_TYPE)]
        public uint ProducerType { get; set; }
    }
}
