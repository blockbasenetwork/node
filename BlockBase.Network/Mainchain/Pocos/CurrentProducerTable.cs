using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class CurrentProducerTable
    {
        [JsonProperty("producer")]
        public string Producer { get; set; }

        [JsonProperty("startproductiontime")]
        public long StartProductionTime { get; set; }
    }
}
