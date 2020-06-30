using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class WarningTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER)]
        public string Producer { get; set; }

        [JsonProperty(EosAtributeNames.WARNING_TYPE)]
        public uint WarningType { get; set; }

        [JsonProperty(EosAtributeNames.WARNING_CREATION_DATE_IN_SECONDS)]
        public ulong WarningCreationDateInSeconds { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER_EXIT_DATE_IN_SECONDS)]
        public ulong ProducerExitDateInSeconds { get; set; }
    }
}
