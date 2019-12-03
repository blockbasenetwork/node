using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Operation;
using EosSharp.Core.Api.v1;
using Microsoft.Extensions.Options;
using System;
using Microsoft.Extensions.Logging;
using EosSharp.Core.Exceptions;

namespace BlockBase.Network.Mainchain
{
    public class MainchainService : IMainchainService
    {
        private EosStub EosStub;
        private NetworkConfigurations NetworkConfigurations;
        private NodeConfigurations NodeConfigurations;
        private MongoDBConfigurations MongoDBConfigurations;
        private readonly ILogger _logger;
        private const int MAX_NUMBER_OF_TRIES = 5;

        public MainchainService(IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations, IOptions<MongoDBConfigurations> mongoDBConfigurations, ILogger<MainchainService> logger)
        {
            NodeConfigurations = nodeConfigurations.Value;
            NetworkConfigurations = networkConfigurations.Value;
            MongoDBConfigurations = mongoDBConfigurations.Value;

            _logger = logger;
            EosStub = new EosStub(NetworkConfigurations.ConnectionExpirationTimeInSeconds, NodeConfigurations.ActivePrivateKey, NetworkConfigurations.EosNet);
        }

        #region Transactions

        public async Task<string> AddCandidature(string chain, string accountName, int worktimeInSeconds, string publicKey, string secretHash) =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_CANDIDATE,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddCandidate(chain, accountName, worktimeInSeconds, publicKey, secretHash)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> AddSecret(string chain, string accountName, string hash) =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_SECRET,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddSecret(chain, accountName, hash)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> AddBlock(string chain, string accountName, Dictionary<string, object> blockHeader) =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_BLOCK,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddBlock(chain, accountName, blockHeader)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> AddEncryptedIps(string chain, string accountName, List<string> encryptedIps) =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_ENCRYPTED_IP,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddEncryptedIps(chain, accountName, encryptedIps)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> NotifyReady(string chain, string accountName) =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.I_AM_READY,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForIAmReady(chain, accountName)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> ProposeBlockVerification(string chain, string accountName, List<string> requestedApprovals, string blockHash) =>
            await TryAgain(async () => await EosStub.ProposeTransaction(
                EosMethodNames.VERIFY_BLOCK,
                NetworkConfigurations.BlockBaseOperationsContract,
                chain,
                accountName,
                CreateDataForVerifyBlock(chain, accountName, blockHash),
                requestedApprovals,
                EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME,
                EosMsigConstants.VERIFY_BLOCK_PERMISSION),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> ApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_APPROVE_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                accountName,
                CreateDataForApproveTransaction(proposerName, proposedTransactionName, accountName, proposalHash),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> ExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_EXEC_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                accountName,
                CreateDataForExecTransaction(proposerName, proposedTransactionName, accountName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> CancelTransaction(string proposerName, string proposedTransactionName, string cancelerName = null, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_CANCEL_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                cancelerName ?? proposerName,
                CreateDataForCancelTransaction(proposerName, proposedTransactionName, cancelerName ?? proposerName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> ClainReward(string chain, string producerName, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.CLAIM_REWARD,
                NetworkConfigurations.BlockBaseTokenContract,
                producerName,
                CreateDataForClaimReward(chain, producerName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> StartChain(string owner, string publicKey, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.START_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForStartChain(owner, publicKey),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> ConfigureChain(string owner, Dictionary<string, object> contractInformation, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.CONFIG_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForConfigurations(owner, contractInformation),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> StartCandidatureTime(string owner, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.START_CANDIDATURE_TIME,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForDeferredTransaction(owner),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> ExitRequest(string owner, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.EXIT_REQUEST,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForExitRequest(owner),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<int> ExecuteChainMaintainerAction(string actionname, string accountname, string permission = "active")
        {
            var timeBeforeSend = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await TryAgain(async () => await EosStub.SendTransaction(
                actionname,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountname,
                CreateDataForDeferredTransaction(accountname),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

            var timeAfterSend = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return (int)(timeAfterSend - timeBeforeSend);
        }

        public async Task<string> AuthorizationAssign(string accountname, List<ProducerInTable> producersNames, string permission = "active", string accountPermission = "active")
        {
            List<AuthorityAccount> accList = new List<AuthorityAccount>();

            foreach (var producer in producersNames)
            {
                AuthorityAccount authAcc = new AuthorityAccount();
                authAcc.account = producer.Key;
                //authAcc.permission = new PermissionLevel() { permission = accountPermission, actor = producer.Key };
                authAcc.weight = 1;
                accList.Add(authAcc);
            }

            Authority newAutorization = new Authority();
            newAutorization.keys = new List<AuthorityKey>();
            newAutorization.waits = new List<AuthorityWait>();
            newAutorization.accounts = accList;
            newAutorization.threshold = 3;

            return await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.UPDATEAUTH,
                EosAtributeNames.EOSIO,
                accountname,
                CreateDataForUpdateAuthorization(accountname, EosMsigConstants.VERIFY_BLOCK_PERMISSION, permission, newAutorization),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
        }

        public async Task<string> LinkAuthorization(string actionName, string accountname, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.LINKAUTH,
                EosAtributeNames.EOSIO,
                accountname,
                CreateDataForLinkAuthorization(accountname, actionName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        #endregion

        #region Table Retrievers

        public async Task<List<ProducerInTable>> RetrieveProducersFromTable(string chain) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PRODUCERS_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);

        public async Task<List<CurrentProducerTable>> RetrieveCurrentProducer(string chain) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CurrentProducerTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CURRENT_PRODUCER_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);

        public async Task<List<CandidateTable>> RetrieveCandidates(string chain) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CandidateTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CANDIDATES_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);

        public async Task<List<BlockheaderTable>> RetrieveBlockheaderList(string chain) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKHEADERS_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);

        public async Task<List<IPAddressTable>> RetrieveIPAddresses(string chain) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<IPAddressTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.IP_ADDRESS_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);

        public async Task<List<TransactionProposalApprovalsTable>> RetrieveApprovals(string proposerAccount) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TransactionProposalApprovalsTable>(EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME, EosMsigConstants.EOSIO_MSIG_APPROVALS_TABLE_NAME, proposerAccount), MAX_NUMBER_OF_TRIES);

        public async Task<List<BlockCountTable>> RetrieveBlockCount(string proposerAccount) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockCountTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKCOUNT_TABLE_NAME, proposerAccount), MAX_NUMBER_OF_TRIES);

        public async Task<ContractInformationTable> RetrieveContractInformation(string chain)
        {
            var listContractInfo = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractInformationTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CONTRACT_INFO_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);
            ContractInformationTable contractInfo = listContractInfo.SingleOrDefault();

            return contractInfo;
        }

        public async Task<ContractStateTable> RetrieveContractState(string chain)
        {
            var listContractState = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractStateTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CONTRACT_STATE_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);
            ContractStateTable contractState = listContractState.SingleOrDefault();

            return contractState;
        }

        public async Task<BlockheaderTable> RetrieveLastBlockFromLastSettlement(string chain)
        {
            var listLastBlock = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKHEADERS_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);
            var lastBlockTable = listLastBlock.Where(b => b.IsLastBlock == true).FirstOrDefault();

            return lastBlockTable;
        }

        public async Task<ClientTable> RetrieveClientTable(string chain)
        {
            var listClient = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ClientTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CLIENT_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);
            ClientTable clientTable = listClient.SingleOrDefault();

            return clientTable;
        }

        public async Task<BlockheaderTable> GetLastSubmittedBlockheader(string chain)
        {
            var lastSubmittedBlock = (await RetrieveBlockheaderList(chain)).LastOrDefault();

            return lastSubmittedBlock;
        }

        public async Task<BlockheaderTable> GetLastValidSubmittedBlockheader(string chain)
        {
            var lastValidSubmittedBlock = (await RetrieveBlockheaderList(chain)).Where(b => b.IsVerified).LastOrDefault();

            return lastValidSubmittedBlock;
        }

        public async Task<BlockheaderTable> GetLastValidSubmittedBlockheaderFromLastProduction(string chain, long currentProductionStartTime)
        {
            var lastValidSubmittedBlock = (await RetrieveBlockheaderList(chain)).Where(b => b.IsVerified && b.Timestamp < currentProductionStartTime).LastOrDefault();

            return lastValidSubmittedBlock;
        }

        public async Task<List<BlockCountTable>> GetBlockCount(string chain)
        {
            var blocks = await RetrieveBlockCount(chain);
            return blocks;
        }

        public async Task<TransactionProposal> RetrieveProposal(string proposerName, string proposalName)
        {
            var proposals = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TransactionProposalTable>(EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME, EosMsigConstants.EOSIO_MSIG_PROPOSAL_TABLE_NAME, proposerName), MAX_NUMBER_OF_TRIES);
            var proposal = proposals?.FirstOrDefault(x => x.ProposalName == proposalName);
            if (proposal != null)
            {
                return new TransactionProposal()
                {
                    ProposalName = proposal.ProposalName,
                    Transaction = await EosStub.UnpackTransaction(proposal.PackedTransaction),
                    TransactionHash = HashHelper.ByteArrayToFormattedHexaString(HashHelper.Sha256Data(HashHelper.FormattedHexaStringToByteArray(proposal.PackedTransaction)))
                };
            }

            return null;
        }

        //TOKEN TABLES

        public async Task<TokenLedgerTable> RetrieveClientTokenLedgerTable(string chain)
        {
            var listLedger = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_LEDGER_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);
            TokenLedgerTable tokenTable = listLedger.Where(b => b.Owner == chain).SingleOrDefault();

            return tokenTable;
        }

        public async Task<TokenAccountTable> RetrieveTokenBalance(string account)
        {
            var tokenTableList = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenAccountTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_TABLE_NAME, account), MAX_NUMBER_OF_TRIES);
            var tokenBalanceTable = tokenTableList.FirstOrDefault();

            return tokenBalanceTable;
        }

        #endregion

        #region Data Helpers

        private Dictionary<string, object> CreateDataForAddCandidate(string chain, string name, int worktimeInSeconds, string publicKey, string secretHash)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.CANDIDATE, name},
                {EosParameterNames.WORK_TIME_IN_SECONDS, worktimeInSeconds},
                {EosParameterNames.PUBLIC_KEY, publicKey},
                {EosParameterNames.SECRET_HASH, secretHash}
            };
        }

        private Dictionary<string, object> CreateDataForAddSecret(string chain, string accountName, string secret)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, chain},
                { EosParameterNames.PRODUCER, accountName},
                { EosParameterNames.SECRET, secret}
            };
        }

        private Dictionary<string, object> CreateDataForAddBlock(string chain, string accountName, Dictionary<string, object> blockHeader)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.PRODUCER, accountName},
                {EosParameterNames.BLOCK, blockHeader}
            };
        }

        private Dictionary<string, object> CreateDataForAddEncryptedIps(string chain, string accountName, List<string> encryptedIps)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, chain},
                { EosParameterNames.NAME, accountName},
                { EosParameterNames.ENCRYPTED_IPS, encryptedIps }
            };
        }

        private Dictionary<string, object> CreateDataForIAmReady(string chain, string accountName)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.PRODUCER, accountName}
            };
        }

        private Dictionary<string, object> CreateDataForVerifyBlock(string chain, string accountName, string blockHash)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.PRODUCER, accountName},
                {EosParameterNames.BLOCK_HASH, blockHash}
            };
        }

        private Dictionary<string, object> CreateDataForApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active")
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.PROPOSER, proposerName },
                { EosParameterNames.PROPOSAL_NAME, proposedTransactionName },
                { EosParameterNames.PROPOSAL_HASH, proposalHash },
                { EosParameterNames.PERMISSION_LEVEL, new PermissionLevel(){
                    actor = accountName,
                    permission = permission
                }}
            };
        }

        private Dictionary<string, object> CreateDataForExecTransaction(string proposerName, string proposedTransactionName, string signerAccountName)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.PROPOSER, proposerName },
                { EosParameterNames.PROPOSAL_NAME, proposedTransactionName },
                { EosParameterNames.EXECUTER, signerAccountName}
            };
        }

        private Dictionary<string, object> CreateDataForCancelTransaction(string proposerName, string proposedTransactionName, string cancelerName)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.PROPOSER, proposerName },
                { EosParameterNames.PROPOSAL_NAME, proposedTransactionName },
                { EosParameterNames.CANCELER, cancelerName}
            };
        }

        private Dictionary<string, object> CreateDataForClaimReward(string owner, string claimer)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.CLAIMER, claimer },
                { EosParameterNames.CONTRACT, NetworkConfigurations.BlockBaseOperationsContract}
            };
        }

        private Dictionary<string, object> CreateDataForDeferredTransaction(string owner)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner }
            };
        }

        private Dictionary<string, object> CreateDataForStartChain(string owner, string publicKey)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PUBLIC_KEY, publicKey },
            };
        }


        private Dictionary<string, object> CreateDataForConfigurations(string owner, Dictionary<string, object> contractInformation)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.CONFIG_INFO_JSON, contractInformation },
            };
        }

        private Dictionary<string, object> CreateDataForExitRequest(string chain)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, chain },
                { EosParameterNames.PRODUCER, NodeConfigurations.AccountName },
            };
        }

        private Dictionary<string, object> CreateDataForUpdateAuthorization(string owner, string newPermission, string parentPermission, Authority newAuth)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.ACCOUNT, owner },
                { EosParameterNames.PERMISSION, newPermission },
                { EosParameterNames.PARENT, parentPermission },
                { EosParameterNames.AUTH, newAuth },
            };
        }

        private Dictionary<string, object> CreateDataForLinkAuthorization(string owner, string action)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.ACCOUNT, owner },
                { EosParameterNames.CODE, NetworkConfigurations.BlockBaseOperationsContract },
                { EosParameterNames.TYPE, action },
                { EosParameterNames.REQUIREMENT, EosMsigConstants.VERIFY_BLOCK_PERMISSION },
            };
        }

        #endregion

        #region Transaction Send Helper

        public async Task<T> TryAgain<T>(Func<Task<OpResult<T>>> func, int maxTry)
        {
            Exception exception = null;

            for (int i = 0; i < maxTry - 1; i++)
            {
                var opResult = await func.Invoke();

                if (opResult.Succeeded)
                {
                    return opResult.Result;
                }
                else
                {
                    exception = opResult.Exception;
                }
            }

            var errorMessage = exception is ApiErrorException apiException ?
                        $"Error sending transaction: {apiException.error.name}" :
                        $"Error sending transaction: {exception}";

            _logger.LogCritical(errorMessage);

            throw exception;
        }

        #endregion
    }
}