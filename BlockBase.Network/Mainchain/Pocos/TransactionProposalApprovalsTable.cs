using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System.Collections.Generic;
using BlockBase.Domain.Eos;
using System;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TransactionProposalApprovalsTable
    {
        [JsonProperty(EosAtributeNames.PROPOSAL_NAME)]
        public string ProposalName { get; set; }

        [JsonProperty(EosAtributeNames.REQUESTED_APPROVALS)]
        public List<Approval> RequestedApprovals { get; set; }

        [JsonProperty(EosAtributeNames.PROVIDED_APPROVALS)]
        public List<Approval> ProvidedApprovals { get; set; }
    }

    public class Approval
    {
        [JsonProperty(EosAtributeNames.LEVEL)]
        public PermissionLevel PermissionLevel { get; set; }

        [JsonProperty(EosAtributeNames.TIME)]
        public DateTime Time { get; set; }
    }
}
