using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain.Pocos;
using EosSharp.Core;
using EosSharp.Core.Api.v1;

namespace BlockBase.Network.Mainchain
{
    public interface IMainchainService
    {
        Task<GetInfoResponse> GetInfo();
        Task<List<string>> GetCurrencyBalance(string smartContractName, string accountName, string symbol = null);
        Task<GetAccountResponse> GetAccount(string accountName);
        Task<AccountStake> GetAccountStake(string sidechain, string accountName);
        Task<string> AddStake(string sidechain, string accountName, string stake);
        Task<string> ClaimStake(string sidechain, string accountName);
        Task<string> AddCandidature(string chain, string accountName, string publicKey, string secretHash, int producerType, int softwareVersion);
        Task<string> RemoveCandidature(string chain, string accountName);
        Task<string> AddSecret(string chain, string accountName, string hash);
        Task<string> AddBlock(string chain, string accountName, Dictionary<string, object> blockHeader);
        Task<string> AddEncryptedIps(string chain, string accountName, List<string> encryptedIps);
        Task<string> UpdatePublicKey(string chain, string accountName, string publicKey);
        Task<string> AddReservedSeats(string chain, List<Dictionary<string, object>> seatsToAdd);
        Task<string> RemoveReservedSeats(string chain, List<string> reservedSeatsToRemove);
        Task<string> NotifyReady(string chain, string accountName);
        Task<string> VerifyBlock(string chain, string producer, string blockHash);
        Task<string> ProposeBlockVerification(string chain, string accountName, List<string> requestedApprovals, string blockHash);
        Task<string> ApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active");
        Task<string> ExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, string permission = "active");
        Task<string> CancelTransaction(string proposerName, string proposedTransactionName, string cancelerName = null, string permission = "active");
        Task<string> StartChain(string owner, string publicKey, string permission = "active");
        Task<string> ConfigureChain(string owner, Dictionary<string, object> contractInformation, List<Dictionary<string, object>> reservedSeats = null, int minimumSoftwareVersion = 1, Dictionary<string, object> blockHeaderToInitalize = null, string permission = "active");
        Task<string> AlterConfigurations(string owner, Dictionary<string, object> configurationsToChange, string permission = "active");
        Task<string> EndChain(string owner, string permission = "active");
        Task<string> StartCandidatureTime(string owner, string permission = "active");
        Task<string> PunishProd(string owner, string permission = "active");
        Task<string> BlacklistProducer(string owner, string producerToPunish, string permission = "active");
        Task<string> RemoveBlacklistedProducer(string owner, string producerToRemove, string permission = "active");
        Task<string> SidechainExitRequest(string sidechainName, string permission = "active");
        Task<int> ExecuteChainMaintainerAction(string actionname, string accountname, string permission = "active");
        Task<string> AuthorizationAssign(string accountname, List<ProducerInTable> producersNames, string authorizationToAssign, string permission = "active", string accountPermission = "active");
        Task<string> LinkAuthorization(string actionName, string accountname, string authorization, string permission = "active");
        Task<string> ClaimReward(string chain, string producerName, string permission = "active");
        Task<string> RequestHistoryValidation(string owner, string producerName, string blockHash, string permission = "active");
        Task<string> SubmitBlockByte(string owner, string producerName, string byteInHexadecimal, string blockHash, string permission = "active");
        Task<string> SignHistoryValidation(string owner, string accountName, string producerToValidade, string byteInHexadecimal, Transaction transaction, string permission = "active");
        Task<string> CreateVerifyBlockTransactionAndAddToContract(string owner, string accountName, string blockHash);
        Task<string> AddBlockByteVerifyTransactionAndSignature(string owner, string accountName, string byteInHexadecimal, byte[] packedTransaction, string permission = "active");
        Task<string> SignVerifyTransactionAndAddToContract(string owner, string account, string blockHash, Transaction transaction, string permission = "active");
        Task<string> BroadcastTransactionWithSignatures(byte[] packedTransaction, List<string> signatures);
        Task<string> AddVerifyTransactionAndSignature(string owner, string accountName, string blockHash, string verifySignature, byte[] verifyBlockTransaction, string permission = "active");
        Task<string> UnlinkAction(string owner, string actionToUnlink, string permission = "active");
        Task<string> DeletePermission(string owner, string permissionToDelete, string permission = "active");

        Task<ClientTable> RetrieveClientTable(string chain);
        Task<List<ProducerInTable>> RetrieveProducersFromTable(string chain);
        Task<CurrentProducerTable> RetrieveCurrentProducer(string chain);
        Task<List<CandidateTable>> RetrieveCandidates(string chain);
        Task<List<BlockheaderTable>> RetrieveBlockheaderList(string chain, int numberOfBlocks);
        Task<List<IPAddressTable>> RetrieveIPAddresses(string chain);
        Task<List<ReservedSeatsTable>> RetrieveReservedSeatsTable(string account);
        Task<TransactionProposalApprovalsTable> RetrieveApprovals(string proposerAccount, string proposalName);
        Task<List<RewardTable>> RetrieveRewardTable(string account);
        Task<ContractInformationTable> RetrieveContractInformation(string chain);
        Task<VersionTable> RetrieveSidechainNodeVersion(string chain);
        Task<ContractStateTable> RetrieveContractState(string chain);
        Task<BlockheaderTable> RetrieveLastBlockFromLastSettlement(string chain, int numberOfBlocks);
        Task<IList<MappedHistoryValidation>> RetrieveHistoryValidation(string chain);
        Task<BlockheaderTable> GetLastSubmittedBlockheader(string chain, int numberOfBlocks);
        Task<BlockheaderTable> GetLastValidSubmittedBlockheader(string chain, int numberOfBlocks);
        Task<BlockheaderTable> GetLastIrreversibleBlockHeader(string chain, int numberOfBlocks);
        Task<List<BlockCountTable>> RetrieveBlockCount(string chain);
        Task<TransactionProposal> RetrieveProposal(string proposerName, string proposalName);
        Task<List<VerifySignature>> RetrieveVerifySignatures(string account);
        Task<List<TokenLedgerTable>> RetrieveAccountStakedSidechains(string accountName);
        Task<List<BlackListTable>> RetrieveBlacklistTable(string chain);
        Task<List<WarningTable>> RetrieveWarningTable(string chain);
        Task<List<AccountPermissionsTable>> RetrieveAccountPermissions(string chain);
        Task<ChangeConfigurationTable> RetrieveConfigurationChanges(string chain);

        void ChangeNetwork();
    }
}