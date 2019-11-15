using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
namespace BlockBase.Network.Mainchain.Pocos
{
    public class Producers
    {
        public string Name { get; set; }
        public uint ActiveSidechains { get; set; }
        public uint TotalSidechains { get; set; }
        public string TotalReward { get; set; }
        public DateTime MemberSince { get; set; }
        public uint NumberOfBlackLists { get; set; }
        public DateTime LastActivity { get; set; }
        public Producers(){}
    }
}