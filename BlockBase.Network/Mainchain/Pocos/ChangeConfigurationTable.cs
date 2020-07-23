using Newtonsoft.Json;
using BlockBase.Domain.Eos;
using System.ComponentModel;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ChangeConfigurationTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.MAX_PAYMENT_PER_BLOCK_VALIDATOR_PRODUCERS)]
        public ulong MaxPaymentPerBlockValidatorProducers { get; set; }

        [JsonProperty(EosAtributeNames.MAX_PAYMENT_PER_BLOCK_HISTORY_PRODUCERS)]
        public ulong MaxPaymentPerBlockHistoryProducers { get; set; }

        [JsonProperty(EosAtributeNames.MAX_PAYMENT_PER_BLOCK_FULL_PRODUCERS)]
        public ulong MaxPaymentPerBlockFullProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_PAYMENT_PER_BLOCK_VALIDATOR_PRODUCERS)]
        public ulong MinPaymentPerBlockValidatorProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_PAYMENT_PER_BLOCK_HISTORY_PRODUCERS)]
        public ulong MinPaymentPerBlockHistoryProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_PAYMENT_PER_BLOCK_FULL_PRODUCERS)]
        public ulong MinPaymentPerBlockFullProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_CANDIDATURE_STAKE)]
        public ulong Stake { get; set; }

        [JsonProperty(EosAtributeNames.NUMBER_OF_VALIDATOR_PRODUCERS_REQUIRED)]
        public uint NumberOfValidatorProducersRequired { get; set; }

        [JsonProperty(EosAtributeNames.NUMBER_OF_HISTORY_PRODUCERS_REQUIRED)]
        public uint NumberOfHistoryProducersRequired { get; set; }

        [JsonProperty(EosAtributeNames.NUMBER_OF_FULL_PRODUCERS_REQUIRED)]
        public uint NumberOfFullProducersRequired { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_TIME_IN_SECONDS)]
        public uint BlockTimeDuration { get; set; }

        [JsonProperty(EosAtributeNames.NUM_BLOCKS_BETWEEN_SETTLEMENTS)]
        public uint BlocksBetweenSettlement { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_SIZE_IN_BYTES)]
        public ulong SizeOfBlockInBytes { get; set; }

        [JsonProperty(EosAtributeNames.CONFIG_CHANGE_TIME_IN_SECONDS)]
        public ulong ConfigChangeTimeInSeconds { get; set; }
    }
}
