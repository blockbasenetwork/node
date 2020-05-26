using System.Net;
using System;
using System.Collections.Generic;

using BlockBase.Domain.Enums;

namespace BlockBase.Domain
{
    public class SidechainState
    {
        public int NumberOfFullProducersCandidatesSoFar { get; set; }

        public int NumberOfHistoryProducersCandidatesSoFar { get; set; }
        
        public int NumberOfValidatorProducersCandidatesSoFar { get; set; }
        
        public string CurrentSidechainStake { get; set; }
        
        public DateTime StakeDepletionEndDate { get; set; }

        public string State { get; set; }

        public Production Production { get; set; }
    }

    public class Production {
        public bool inProduction { get; set; }

        public int CurrentNumberOfFullProducersInChain { get; set; }
        
        public int CurrentNumberOfHistoryProducersInChain { get; set; }
        
        public int CurrentNumberOfValidatorProducersInChain { get; set; }
    }
}
