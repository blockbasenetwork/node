using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
namespace BlockBase.Network.Mainchain.Pocos
{

    public class ProducerDetail {
        public string SidechainName { get; set; }
        public string SidechainState { get; set; }
        public bool IsSidechainInProduction { get; set; }
        public string ProducerStateInChain { get; set; }
        public DateTime WorkTime { get; set; }
        public bool IsRewardAvailable { get; set; }
        public string StakeCommited { get; set; }
            
        public ProducerDetail(){}
    }
}