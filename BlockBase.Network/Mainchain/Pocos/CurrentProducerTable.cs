using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class CurrentProducerTable
    {
        [JsonProperty(EosAtributeNames.PRODUCER)]
        public string Producer { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCTION_START_DATE_IN_SECONDS)]
        public long StartProductionTime { get; set; }

        [JsonProperty(EosAtributeNames.HAS_PRODUCED_BLOCK)]
        public bool HasProducedBlock { get; set; }
    }
}
