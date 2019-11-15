using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace BlockBase.Network.History.Pocos
{
    public class SidechainBlackList
    {  
        public string ProducerName { get; set; }
        public string StakeLost { get; set; }
        public DateTime Date { get; set; }
        public string BlockHeader { get; set; }
        public uint BlockNumber { get; set; }
        public SidechainBlackList(){}
    }
}