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
        public List<PermissionLevel> RequestedApprovals { get; set; }

        [JsonProperty("provided_approvals")]
        public List<PermissionLevel> ProvidedApprovals { get; set; }
    }
}
