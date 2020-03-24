using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain.Pocos;
using EosSharp.Core.Api.v1;

namespace BlockBase.Network.Mainchain
{
    public interface IMainchainService
    {
        Task<GetAccountResponse> GetAccount(string accountName);
        Task<string> AddCandidature(string chain, string accountName, int worktimeInSeconds, string publicKey, string secretHash, int producerType);
        Task<string> AddSecret(string chain, string accountName, string hash);
        Task<string> AddBlock(string chain, string accountName, Dictionary<string, object> blockHeader);
        Task<string> SafeAddBlock(string chain, string accountName, Dictionary<string, object> blockHeader, int limit);
        Task<string> AddEncryptedIps(string chain, string accountName, List<string> encryptedIps);
        Task<string> NotifyReady(string chain, string accountName);
        Task<string> ProposeBlockVerification(string chain, string accountName, List<string> requestedApprovals, string blockHash);
        Task<string> ApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active");
        Task<string> ExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, string permission = "active");
        Task<string> SafeExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, int limit, string permission = "active");
        Task<string> CancelTransaction(string proposerName, string proposedTransactionName, string cancelerName = null, string permission = "active");
        Task<string> StartChain(string owner, string publicKey, string permission = "active");
        Task<string> ConfigureChain(string owner, Dictionary<string, object> contractInformation, List<string> reservedSeats = null, string permission = "active");
        Task<string> EndChain(string owner, string permission = "active");
        Task<string> StartCandidatureTime(string owner, string permission = "active");
        Task<string> PunishProd(string owner, string permission = "active");
        Task<string> BlacklistProducer(string owner, string producerToPunish, string permission = "active");
        Task<int> ExecuteChainMaintainerAction(string actionname, string accountname, string permission = "active");
        Task<string> AuthorizationAssign(string accountname, List<ProducerInTable> producersNames, string permission = "active", string accountPermission = "active");
        Task<string> LinkAuthorization(string actionName ,string accountname, string permission = "active");
        Task<string> ClaimReward(string chain, string producerName, string permission = "active");
        Task<string> RequestHistoryValidation(string owner, string producerName, string blockHash, string permission = "active");
        Task<string> AddBlockByte(string owner, string producerName, string byteInHexadecimal, string permission = "active");
        Task<string> ProposeHistoryValidation(string chain, string accountName, List<string> requestedApprovals, string proposalName);

        Task<ClientTable> RetrieveClientTable(string chain);
        Task<List<ProducerInTable>> RetrieveProducersFromTable(string chain);
        Task<List<CurrentProducerTable>> RetrieveCurrentProducer(string chain);
        Task<List<CandidateTable>> RetrieveCandidates(string chain);
        Task<List<BlockheaderTable>> RetrieveBlockheaderList(string chain, int numberOfBlocks);
        Task<List<IPAddressTable>> RetrieveIPAddresses(string chain);
        Task<TransactionProposalApprovalsTable> RetrieveApprovals(string proposerAccount, string proposalName);
        Task<List<RewardTable>> RetrieveRewardTable(string account);
        Task<ContractInformationTable> RetrieveContractInformation(string chain);
        Task<ContractStateTable> RetrieveContractState(string chain);
        Task<BlockheaderTable> RetrieveLastBlockFromLastSettlement(string chain, int numberOfBlocks);
        Task<HistoryValidationTable> RetrieveHistoryValidationTable(string chain);
        Task<BlockheaderTable> GetLastSubmittedBlockheader(string chain, int numberOfBlocks);
        Task<BlockheaderTable> GetLastValidSubmittedBlockheader(string chain, int numberOfBlocks);
        Task<List<BlockCountTable>> RetrieveBlockCount(string chain);
        Task<TransactionProposal> RetrieveProposal(string proposerName, string proposalName);
        Task<TokenLedgerTable> RetrieveClientTokenLedgerTable(string account);
        Task<TokenAccountTable> RetrieveTokenBalance(string account);
    }
}