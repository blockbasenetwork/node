using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class RequesterConfigurations
    {
        public ulong MaxPaymentPerBlockValidatorProducers { get; set; }
        public ulong MaxPaymentPerBlockHistoryProducers { get; set; }
        public ulong MaxPaymentPerBlockFullProducers { get; set; }
        public ulong MinimumPaymentPerBlockValidatorProducers { get; set; }
        public ulong MinimumPaymentPerBlockHistoryProducers { get; set; }
        public ulong MinimumPaymentPerBlockFullProducers { get; set; }
        public ulong MinimumCandidatureStake { get; set; }
        public uint NumberOfValidatorProducersRequired { get; set; }
        public uint NumberOfHistoryProducersRequired { get; set; }
        public uint NumberOfFullProducersRequired { get; set; }
        public uint BlockTimeInSeconds { get; set; }
        public uint NumberOfBlocksBetweenSettlements { get; set; }
        public uint BlockSizeInBytes { get; set; }
    }
}
