using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    // public class RequesterConfigurations
    // {
    //     public double MaxPaymentPerBlockValidatorProducers { get; set; }
    //     public double MaxPaymentPerBlockHistoryProducers { get; set; }
    //     public double MaxPaymentPerBlockFullProducers { get; set; }
    //     public double MinimumPaymentPerBlockValidatorProducers { get; set; }
    //     public double MinimumPaymentPerBlockHistoryProducers { get; set; }
    //     public double MinimumPaymentPerBlockFullProducers { get; set; }
    //     public double MinimumCandidatureStake { get; set; }
    //     public uint NumberOfValidatorProducersRequired { get; set; }
    //     public uint NumberOfHistoryProducersRequired { get; set; }
    //     public uint NumberOfFullProducersRequired { get; set; }
    //     public uint BlockTimeInSeconds { get; set; }
    //     public uint NumberOfBlocksBetweenSettlements { get; set; }
    //     public uint BlockSizeInBytes { get; set; }
    //     public List<string> ReservedProducerSeats { get; set; }
    // }

    public class RequesterConfigurations
    {

        public NodeConfig ValidatorNodes { get; set; }
        public NodeConfig HistoryNodes { get; set; }
        public NodeConfig FullNodes { get; set; }

        public double RequesterStake { get; set; }
        public double MinimumProducerStake { get; set; }
        public uint BlockTimeInSeconds { get; set; }
        public uint NumberOfBlocksBetweenSettlements { get; set; }
        public uint MaxBlockSizeInBytes { get; set; }
        public List<string> ReservedProducerSeats { get; set; }
    }

    public class NodeConfig {
        public uint RequiredNumber { get; set; }
        public double MaxPaymentPerBlock { get; set; }
        public double MinPaymentPerBlock { get; set; }
    }
}
