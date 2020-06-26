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

        [JsonProperty(EosAtributeNames.WARNING_TYPE)]
        public uint Warning { get; set; }

        [JsonProperty(EosAtributeNames.WORK_DURATION_IN_SECONDS)]
        public ulong WorkTimeInSeconds { get; set; }

        [JsonProperty(EosAtributeNames.SIDECHAIN_START_DATE_IN_SECONDS)]
        public ulong StartInSidechainDate { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER_TYPE)]
        public uint ProducerType { get; set; }

        [JsonProperty(EosAtributeNames.IS_READY_TO_PRODUCE)]
        public bool IsReadyToProduce { get; set; }
    }
}
