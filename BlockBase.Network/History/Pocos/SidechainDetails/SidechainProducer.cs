using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace BlockBase.Network.History.Pocos
{
    public class SidechainProducer
    {  
        public string ProducerName { get; set; }
        public DateTime Date { get; set; }
        public long WorkTime { get; set; }
        public string StakeCommited { get; set; }
        public string Warning { get; set; }
        public uint BlocksProduced { get; set; }
        public SidechainProducer(){}
    }
}