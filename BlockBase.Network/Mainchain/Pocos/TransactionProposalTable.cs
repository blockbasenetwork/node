using BlockBase.Domain.Eos;
using Newtonsoft.Json;


namespace BlockBase.Network.Mainchain.Pocos
{
    public class TransactionProposalTable
    {
        [JsonProperty(EosAtributeNames.PROPOSAL_NAME)]
        public string ProposalName { get; set; }

        [JsonProperty(EosAtributeNames.PACKED_TRANSACTION)]
        public string PackedTransaction { get; set; }
    }
}
