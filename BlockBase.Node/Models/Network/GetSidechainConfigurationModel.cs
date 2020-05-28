using Newtonsoft.Json;
using BlockBase.Domain.Eos;
using System.ComponentModel;
using System;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class GetSidechainConfigurationModel
    {
        public string account_name { get; set; }

        [JsonProperty(EosAtributeNames.MAX_PAYMENT_PER_BLOCK_VALIDATOR_PRODUCERS)]
        public decimal MaxPaymentPerBlockValidatorProducers { get; set; }

        [JsonProperty(EosAtributeNames.MAX_PAYMENT_PER_BLOCK_HISTORY_PRODUCERS)]
        public decimal MaxPaymentPerBlockHistoryProducers { get; set; }

        [JsonProperty(EosAtributeNames.MAX_PAYMENT_PER_BLOCK_FULL_PRODUCERS)]
        public decimal MaxPaymentPerBlockFullProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_PAYMENT_PER_BLOCK_VALIDATOR_PRODUCERS)]
        public decimal MinPaymentPerBlockValidatorProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_PAYMENT_PER_BLOCK_HISTORY_PRODUCERS)]
        public decimal MinPaymentPerBlockHistoryProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_PAYMENT_PER_BLOCK_FULL_PRODUCERS)]
        public decimal MinPaymentPerBlockFullProducers { get; set; }

        [JsonProperty(EosAtributeNames.MIN_CANDIDATURE_STAKE)]
        public decimal Stake { get; set; }

        [JsonProperty(EosAtributeNames.NUMBER_OF_VALIDATOR_PRODUCERS_REQUIRED)]
        public uint NumberOfValidatorProducersRequired { get; set; }

        [JsonProperty(EosAtributeNames.NUMBER_OF_HISTORY_PRODUCERS_REQUIRED)]
        public uint NumberOfHistoryProducersRequired { get; set; }

        [JsonProperty(EosAtributeNames.NUMBER_OF_FULL_PRODUCERS_REQUIRED)]
        public uint NumberOfFullProducersRequired { get; set; }

        [JsonProperty("candidature-phase-end-date")]
        public DateTime CandidatureEndDate { get; set; }

        [JsonProperty(EosAtributeNames.SECRET_SENDING_PHASE_END_DATE_IN_SECONDS)]
        public long SecretEndDate { get; set; }

        [JsonProperty(EosAtributeNames.IP_SENDING_PHASE_END_DATE_IN_SECONDS)]
        public long SendEndDate { get; set; }

        [JsonProperty(EosAtributeNames.IP_RETRIEVAL_PHASE_END_DATE_IN_SECONDS)]
        public long ReceiveEndDate { get; set; }

        [JsonProperty(EosAtributeNames.CANDIDATURE_PHASE_DURATION_IN_SECONDS)]
        public uint CandidatureTime { get; set; }

        [JsonProperty(EosAtributeNames.IP_SENDING_PHASE_DURATION_IN_SECONDS)]
        public uint SendTime { get; set; }

        [JsonProperty(EosAtributeNames.IP_RETRIEVAL_PHASE_DURATION_IN_SECONDS)]
        public uint ReceiveTime { get; set; }

        [JsonProperty(EosAtributeNames.SECRET_SENDING_PHASE_DURATION_IN_SECONDS)]
        public uint SendSecretTime { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_TIME_IN_SECONDS)]
        public uint BlockTimeDuration { get; set; }

        [JsonProperty(EosAtributeNames.NUM_BLOCKS_BETWEEN_SETTLEMENTS)]
        public uint BlocksBetweenSettlement { get; set; }

        [JsonProperty(EosAtributeNames.BLOCK_SIZE_IN_BYTES)]
        public uint SizeOfBlockInBytes { get; set; }

    }
}
