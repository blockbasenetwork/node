using EosSharp;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TransactionProposalTable
    {
        [JsonProperty("proposal_name")]
        public string ProposalName { get; set; }

        [JsonProperty("packed_transaction")]
        public string PackedTransaction { get; set; }
    }
}
