using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockBase.Domain.Results
{

    public class TrackerSidechain
    {
        public string Name { get; set; }
        public string Network { get; set; }
        public string State { get; set; }
        public bool IsProducing { get; set; }
        public ProducerType FullProducers { get; set; }
        public ProducerType HistoryProducers { get; set; }
        public ProducerType ValidatorProducers { get; set; }
    }

    public class ProducerType {
        public int RequiredNumberOfProducers { get; set; }
        public string PaymentForFullBlock { get; set; }
        public string PaymentForEmptyBlock { get; set; }
        public int AvailableSeats { get; set; }
        
    }
}
