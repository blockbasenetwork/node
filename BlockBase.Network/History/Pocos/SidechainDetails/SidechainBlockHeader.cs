using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace BlockBase.Network.History.Pocos
{
    public class SidechainBlockHeader
    {  
        public string BlockHash { get; set; }
        public string PreviousBlockHash { get; set; }
        public uint TransactionsNumber { get; set; }
        public uint BlockNumber { get; set; }
        public string Producer { get; set; }
        public DateTime CreationDate { get; set; }
        public SidechainBlockHeader(){}
    }
}