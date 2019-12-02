using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain.Pocos;

namespace BlockBase.Network.Mainchain
{
    public interface IMainchainService
    {
        Task<string> AddCandidature(string chain, string accountName, int worktimeInSeconds, string publicKey, string secretHash);
        Task<string> AddSecret(string chain, string accountName, string hash);
        Task<string> AddBlock(string chain, string accountName, Dictionary<string, object> blockHeader);
        Task<string> AddEncryptedIps(string chain, string accountName, List<string> encryptedIps);
        Task<string> NotifyReady(string chain, string accountName);
        Task<string> ProposeBlockVerification(string chain, string accountName, List<string> requestedApprovals, string blockHash);
        Task<string> ApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active");
        Task<string> ExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, string permission = "active");
        Task<string> CancelTransaction(string proposerName, string proposedTransactionName, string cancelerName = null, string permission = "active");
        Task<string> StartChain(string owner, string publicKey, string permission = "active");
        Task<string> ConfigureChain(string owner, Dictionary<string, object> contractInformation, string permission = "active");
        Task<string> StartCandidatureTime(string owner, string permission = "active");
        Task<int> ExecuteChainMaintainerAction(string actionname, string accountname, string permission = "active");
        Task<string> AuthorizationAssign(string accountname, List<ProducerInTable> producersNames, string permission = "active", string accountPermission = "active");
        Task<string> LinkAuthorization(string actionName ,string accountname, string permission = "active");

        Task<ClientTable> RetrieveClientTable(string smartContractAccount);
        Task<List<ProducerInTable>> RetrieveProducersFromTable(string smartContractAccount);
        Task<List<CurrentProducerTable>> RetrieveCurrentProducer(string smartContractAccount);
        Task<List<CandidateTable>> RetrieveCandidates(string smartContractAccount);
        Task<List<BlockheaderTable>> RetrieveBlockheaderList(string smartContractAccount);
        Task<List<IPAddressTable>> RetrieveIPAddresses(string smartContractAccount);
        Task<List<TransactionProposalApprovalsTable>> RetrieveApprovals(string proposerAccount);
        Task<ContractInformationTable> RetrieveContractInformation(string smartContractAccount);
        Task<ContractStateTable> RetrieveContractState(string smartContractAccount);
        Task<BlockheaderTable> RetrieveLastBlockFromLastSettlement(string smartContractAccount);
        Task<BlockheaderTable> GetLastSubmittedBlockheader(string smartContractAccount);
        Task<BlockheaderTable> GetLastValidSubmittedBlockheader(string chain);
        Task<BlockheaderTable> GetLastValidSubmittedBlockheaderFromLastProduction(string chain, long currentProductionStartTime);
        Task<List<BlockCountTable>> GetBlockCount(string chain);
        Task<TransactionProposal> RetrieveProposal(string proposerName, string proposalName);
        Task<TokenLedgerTable> RetrieveClientTokenLedgerTable(string account);
        Task<TokenAccountTable> RetrieveTokenBalance(string account);
    }
}