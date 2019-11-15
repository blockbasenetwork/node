using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
namespace BlockBase.Network.Mainchain.Pocos
{
    public class SidechainDetail
    {
        public string State { get; set; }
        public bool IsProducting { get; set; }
        public string MinCandidateStake { get; set; }
        public uint BlockThreshold { get; set; }
        public string CurrentBlockHeaderHash { get; set; }
        public string Reward { get; set; }
        public uint NeededProducerNumber { get; set; }
        public uint ActualProducerNumber { get; set; }
        public string TotalStake { get; set; }
        public uint TotalBlocks { get; set; }
        public string CurrentProducer { get; set; } 
        public SidechainDetail(){}
    }
}