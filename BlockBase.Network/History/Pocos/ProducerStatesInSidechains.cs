using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
namespace BlockBase.Network.Mainchain.Pocos
{
    public class ProducerStatesInSidechains
    {
        public string CreateName;
        public string State;
        public bool IsProducting;
        public string HisStateInChain;
        public string Worktime;
        public ProducerStatesInSidechains(){}
    }
}