using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class RequesterConfigurations
    {
        public double MaxPaymentPerBlockValidatorProducers { get; set; }
        public double MaxPaymentPerBlockHistoryProducers { get; set; }
        public double MaxPaymentPerBlockFullProducers { get; set; }
        public double MinimumPaymentPerBlockValidatorProducers { get; set; }
        public double MinimumPaymentPerBlockHistoryProducers { get; set; }
        public double MinimumPaymentPerBlockFullProducers { get; set; }
        public double MinimumCandidatureStake { get; set; }
        public uint NumberOfValidatorProducersRequired { get; set; }
        public uint NumberOfHistoryProducersRequired { get; set; }
        public uint NumberOfFullProducersRequired { get; set; }
        public uint BlockTimeInSeconds { get; set; }
        public uint NumberOfBlocksBetweenSettlements { get; set; }
        public uint BlockSizeInBytes { get; set; }
        public List<string> ReservedProducerSeats { get; set; }
    }
}
