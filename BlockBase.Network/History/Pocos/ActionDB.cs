using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace BlockBase.Network.History.Pocos
{
    public class ActionDB
    {   [JsonProperty("actions")]
        public List<ActionInfo> ActionList { get; set; }
    }

    public class ActionInfo
    {
        [JsonProperty("block_num")]
        public int BlockNumber { get; set; }
        [JsonProperty("action_trace")]
        public ActionTrace ActionTrace { get; set; }
        [JsonProperty("block_time")]
        public DateTime BlockTime { get; set; }
    }

    public class ActionTrace {

        [JsonProperty("act")]
        public Act ActionInformation { get; set; }
    }

    public class Act {
        [JsonProperty("account")]
        public string ContractAccount { get; set; }
        [JsonProperty("name")]
        public string ActionName { get; set; }
        [JsonProperty("authorization")]
        public List<Authorization> AuthorizationList { get; set; }
        public Data Data { get; set; }
       
    }
    public class Authorization {
        [JsonProperty("actor")]
        public string SenderAccount { get; set; }
    }
    public class Data {
        [JsonProperty("owner")]
        public string owner { get; set; }
        [JsonProperty("sidechain")]
        public string sidechain { get; set; }
        [JsonProperty("stake")]
        public string stake { get; set; }
    }
}