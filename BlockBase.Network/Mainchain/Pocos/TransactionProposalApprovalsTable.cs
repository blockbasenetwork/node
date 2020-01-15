using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TransactionProposalApprovalsTable
    {
        [JsonProperty("proposal_name")]
        public string ProposalName { get; set; }

        [JsonProperty("requested_approvals")]
        public List<Approval> RequestedApprovals { get; set; }

        [JsonProperty("provided_approvals")]
        public List<Approval> ProvidedApprovals { get; set; }
    }

    public class Approval
    {
        [JsonProperty("level")]
        public PermissionLevel PermissionLevel { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }
    }
}
