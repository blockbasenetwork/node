using System.Net;
using System;
using System.Collections.Generic;

using BlockBase.Domain.Enums;

namespace BlockBase.Domain
{
    public class SidechainState
    {
        public string CurrentRequesterStake { get; set; }

        public DateTime StakeDepletionEndDate { get; set; }

        public string State { get; set; }

        public bool InProduction { get; set; }

        public ReservedSeats ReservedSeats { get; set; }
        
        public SidechainProducersInfo FullProducersInfo { get; set; }

        public SidechainProducersInfo HistoryProducersInfo { get; set; }

        public SidechainProducersInfo ValidatorProducersInfo { get; set; }
    }

    public class SidechainProducersInfo {
        public int NumberOfProducersRequired { get; set; }
        public int NumberOfProducersInChain { get; set; }
        public int CandidatesWaitingForSeat { get; set; }
        public int NumberOfSlotsTakenByReservedSeats { get; set; }
    }

    public class ReservedSeats {

        public int TotalNumber { get; set; }
        
        public int SlotsTaken { get; set; }
        
        public int SlotsStillAvailable { get; set; }

    }
}