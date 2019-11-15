using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ContractInformationTable
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("paymentperblock")]
        public uint Payment { get; set; }

        [JsonProperty("minimumcandidatestake")]
        public uint Stake { get; set; }

        [JsonProperty("requirednumberofproducers")]
        public uint ProducersNumber { get; set; }

        [JsonProperty("candidatureenddate")]
        public long CandidatureEndDate { get; set; }

        [JsonProperty("secretenddate")]
        public long SecretEndDate { get; set; }

        [JsonProperty("ipsendenddate")]
        public long SendEndDate { get; set; }

        [JsonProperty("ipreceiveenddate")]
        public long ReceiveEndDate { get; set; }

        [JsonProperty("candidaturetime")]
        public uint CandidatureTime { get; set; }

        [JsonProperty("ipsendtime")]
        public uint SendTime { get; set; }

        [JsonProperty("ipreceivetime")]
        public uint ReceiveTime { get; set; }

        [JsonProperty("sendsecrettime")]
        public uint SendSecretTime { get; set; }

        [JsonProperty("blocktimeduration")]
        public uint BlockTimeDuration { get; set; }

        [JsonProperty("blocksbetweensettlement")]
        public uint BlocksBetweenSettlement { get; set; }

        [JsonProperty("sizeofblockinbytes")]
        public uint SizeOfBlockInBytes { get; set; }

    }
}
