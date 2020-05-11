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
using EosSharp.Core;
using EosSharp.Core.Helpers;

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
        private const int TRANSACTION_EXPIRATION = 20;

        public MainchainService(IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations, IOptions<MongoDBConfigurations> mongoDBConfigurations, ILogger<MainchainService> logger)
        {
            NodeConfigurations = nodeConfigurations.Value;
            NetworkConfigurations = networkConfigurations.Value;
            MongoDBConfigurations = mongoDBConfigurations.Value;

            _logger = logger;
            EosStub = new EosStub(TRANSACTION_EXPIRATION, NodeConfigurations.ActivePrivateKey, NetworkConfigurations.EosNet);
        }

        public async Task<GetAccountResponse> GetAccount(string accountName)
            => await TryAgain(async () => await EosStub.GetAccount(accountName), NetworkConfigurations.MaxNumberOfConnectionRetries);

        #region Transactions

        public async Task<string> AddCandidature(string chain, string accountName, int worktimeInSeconds, string publicKey, string secretHash, int producerType) =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_CANDIDATE,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddCandidate(chain, accountName, worktimeInSeconds, publicKey, secretHash, producerType)),
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

        public async Task<string> SafeAddBlock(string chain, string accountName, Dictionary<string, object> blockHeader, int limit) =>
            await EosStub.SendSafeTransaction<string>(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_BLOCK,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddBlock(chain, accountName, blockHeader)),
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.BLOCKHEADERS_TABLE_NAME,
                EosAtributeNames.BLOCK_HASH,
                chain,
                limit
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
                chain,
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

        public async Task<string> SafeExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, int limit, string permission = "active") =>
            await EosStub.SendSafeTransaction<long>(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_EXEC_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                accountName,
                CreateDataForExecTransaction(proposerName, proposedTransactionName, accountName),
                permission),
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.BLOCKHEADERS_TABLE_NAME,
                EosAtributeNames.IS_VERIFIED,
                proposedTransactionName,
                limit
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

        public async Task<string> ClaimReward(string chain, string producerName, string permission = "active") =>
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

        public async Task<string> ConfigureChain(string owner, Dictionary<string, object> contractInformation, List<string> reservedSeats = null, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.CONFIG_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForConfigurations(owner, contractInformation, reservedSeats),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> EndChain(string owner, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.END_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForDeferredTransaction(owner),
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

        public async Task<string> BlacklistProducer(string owner, string producerToBlacklist, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.BLACKLIST_PRODUCERS,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForBlackListProd(owner, producerToBlacklist),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> PunishProd(string owner, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.PUNISH_PRODUCERS,
                NetworkConfigurations.BlockBaseTokenContract,
                owner,
                CreateDataForProdPunish(owner),
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

        public async Task<string> AuthorizationAssign(string accountname, List<ProducerInTable> producersNames, string authorizationToAssign, string permission = "active", string accountPermission = "active")
        {
            List<AuthorityAccount> accList = new List<AuthorityAccount>();

            foreach (var producer in producersNames)
            {
                AuthorityAccount authAcc = new AuthorityAccount();
                authAcc.permission = new PermissionLevel() { permission = accountPermission, actor = producer.Key };
                authAcc.weight = 1;
                accList.Add(authAcc);
            }

            Authority newAutorization = new Authority();
            newAutorization.keys = new List<AuthorityKey>();
            newAutorization.waits = new List<AuthorityWait>();
            newAutorization.accounts = accList;
            newAutorization.threshold = (uint)(producersNames.Count() / 2) + 1;

            return await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.UPDATEAUTH,
                EosAtributeNames.EOSIO,
                accountname,
                CreateDataForUpdateAuthorization(accountname, authorizationToAssign, permission, newAutorization),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
        }

        public async Task<string> LinkAuthorization(string actionName, string accountname, string authorization, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.LINKAUTH,
                EosAtributeNames.EOSIO,
                accountname,
                CreateDataForLinkAuthorization(accountname, actionName, authorization),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> RequestHistoryValidation(string owner, string producerName, string blockHash, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.REQUEST_HISTORY_VALIDATION,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForRequestHistoryValidation(owner, producerName, blockHash),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
        );

        public async Task<string> AddBlockByte(string owner, string producerName, string byteInHexadecimal, string permission = "active") =>
         await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_BLOCK_BYTE,
                NetworkConfigurations.BlockBaseOperationsContract,
                producerName,
                CreateDataForAddBlockByte(owner, producerName, byteInHexadecimal),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
        );

        public async Task<string> ProposeHistoryValidation(string owner, string accountName, List<string> requestedApprovals, string proposalName) =>
            await TryAgain(async () => await EosStub.ProposeTransaction(
                EosMethodNames.HISTORY_VALIDATE,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                accountName,
                CreateDataForValidateHistory(owner, accountName),
                requestedApprovals,
                proposalName,
                EosMsigConstants.VERIFY_HISTORY_PERMISSION),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );

        public async Task<string> CreateVerifyBlockTransactionAndAddToContract(string owner, string accountName, string blockHash, string permission = "acctive")
        {
            var transaction = new Transaction()
            {
                expiration = DateTime.UtcNow.AddDays(1),
                actions = new List<EosSharp.Core.Api.v1.Action>()
                {
                    new EosSharp.Core.Api.v1.Action()
                    {
                        account = NetworkConfigurations.BlockBaseOperationsContract,
                        authorization = new List<PermissionLevel>()
                        {
                            new PermissionLevel() {actor = owner, permission = EosMsigConstants.VERIFY_BLOCK_PERMISSION }
                        },
                        name = EosMethodNames.VERIFY_BLOCK,
                        data = CreateDataForVerifyBlock(owner, accountName, blockHash)
                    }
                }
            };

            var signedTransaction = await EosStub.SignTransaction(transaction, NodeConfigurations.ActivePublicKey);
            _logger.LogDebug($"sign: {signedTransaction.Signatures.FirstOrDefault()}");
            signedTransaction = await EosStub.SignTransaction(transaction, NodeConfigurations.ActivePublicKey);
            _logger.LogDebug($"sign: {signedTransaction.Signatures.FirstOrDefault()}");
            return await AddVerifyTransactionAndSignature(owner, accountName, blockHash, signedTransaction.Signatures.FirstOrDefault(), signedTransaction.PackedTransaction);
        }

        public async Task<string> SignVerifyTransactionAndAddToContract(string owner, string account, string blockHash, Transaction transaction, string permission = "active")
        {
            var signedTransaction = await EosStub.SignTransaction(transaction, NodeConfigurations.ActivePublicKey);
            _logger.LogDebug($"sign: {signedTransaction.Signatures.FirstOrDefault()}");
            signedTransaction = await EosStub.SignTransaction(transaction, NodeConfigurations.ActivePublicKey);
            _logger.LogDebug($"sign: {signedTransaction.Signatures.FirstOrDefault()}");
            return await AddVerifyTransactionAndSignature(owner, account, blockHash, signedTransaction.Signatures.FirstOrDefault(), signedTransaction.PackedTransaction);
        }

        public async Task<string> BroadcastTransactionWithSignatures(byte[] packedTransaction, List<string> signatures)
        {
            var signedTransaction = new SignedTransaction()
            {
                PackedTransaction = packedTransaction,
                Signatures = signatures.Distinct()
            };

            return await EosStub.BroadcastTransaction(signedTransaction);
        }

        public async Task<string> AddVerifyTransactionAndSignature(string owner, string accountName, string blockHash, string verifySignature, byte[] verifyBlockTransaction, string permission = "active") =>
            await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_VERIFY_SIGNATURE,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddVerifyTransactionAndSignature(owner, accountName, blockHash, verifySignature, verifyBlockTransaction),
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

        public async Task<List<BlockheaderTable>> RetrieveBlockheaderList(string chain, int numberOfBlocks) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKHEADERS_TABLE_NAME, chain, numberOfBlocks), MAX_NUMBER_OF_TRIES);

        public async Task<List<IPAddressTable>> RetrieveIPAddresses(string chain) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<IPAddressTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.IP_ADDRESS_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);

        public async Task<List<BlockCountTable>> RetrieveBlockCount(string proposerAccount) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockCountTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKCOUNT_TABLE_NAME, proposerAccount), MAX_NUMBER_OF_TRIES);

        public async Task<List<RewardTable>> RetrieveRewardTable(string account) =>
            await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<RewardTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PENDING_REWARD_TABLE, account), MAX_NUMBER_OF_TRIES);


        public async Task<TransactionProposalApprovalsTable> RetrieveApprovals(string proposerAccount, string proposalName)
        {
            var list = await TryAgain(async () => (await EosStub.GetRowsFromSmartContractTable<TransactionProposalApprovalsTable>(EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME, EosMsigConstants.EOSIO_MSIG_APPROVALS_TABLE_NAME, proposerAccount)), MAX_NUMBER_OF_TRIES);
            TransactionProposalApprovalsTable transactionProposalApprovalsTable = list.Where(t => t.ProposalName == proposalName).SingleOrDefault();

            return transactionProposalApprovalsTable;
        }

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

        public async Task<BlockheaderTable> RetrieveLastBlockFromLastSettlement(string chain, int numberOfBlocks)
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

        public async Task<BlockheaderTable> GetLastSubmittedBlockheader(string chain, int numberOfBlocks)
        {
            var lastSubmittedBlock = (await RetrieveBlockheaderList(chain, numberOfBlocks)).LastOrDefault();

            return lastSubmittedBlock;
        }

        public async Task<BlockheaderTable> GetLastValidSubmittedBlockheader(string chain, int numberOfBlocks)
        {
            var lastValidSubmittedBlock = (await RetrieveBlockheaderList(chain, numberOfBlocks)).Where(b => b.IsVerified).LastOrDefault();

            return lastValidSubmittedBlock;
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

        public async Task<List<VerifySignature>> RetrieveVerifySignatures(string account)
        {
            var verifySignaturesList = new List<VerifySignature>();
            var verifySignaturesTable = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<VerifySignatureTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.VERIFY_SIGNATURE_TABLE, account), MAX_NUMBER_OF_TRIES);

            foreach(var verifySignature in verifySignaturesTable)
            {
                var mappedVerifySignature = new VerifySignature()
                {
                    Account = verifySignature.Key,
                    BlockHash = verifySignature.BlockHash,
                    Signature = verifySignature.VerifySignature,
                    PackedTransaction = SerializationHelper.HexStringToByteArray(verifySignature.PackedTransaction),
                    Transaction = string.IsNullOrEmpty(verifySignature.PackedTransaction) ? null : await EosStub.UnpackTransaction(verifySignature.PackedTransaction)
                };

                verifySignaturesList.Add(mappedVerifySignature);
            }

            return verifySignaturesList;
        }
            
        public async Task<HistoryValidationTable> RetrieveHistoryValidationTable(string chain)
        {
            var listValidationTable = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<HistoryValidationTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.HISTORY_VALIDATION_TABLE, chain), MAX_NUMBER_OF_TRIES);
            HistoryValidationTable historyValidationTable = listValidationTable.SingleOrDefault();

            return historyValidationTable;
        }


        //TOKEN TABLES

        public async Task<TokenLedgerTable> RetrieveClientTokenLedgerTable(string chain)
        {
            var listLedger = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_LEDGER_TABLE_NAME, chain), MAX_NUMBER_OF_TRIES);
            TokenLedgerTable tokenTable = listLedger.Where(b => b.Sidechain == chain).SingleOrDefault();

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

        private Dictionary<string, object> CreateDataForAddCandidate(string chain, string name, int worktimeInSeconds, string publicKey, string secretHash, int producerType)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.CANDIDATE, name},
                {EosParameterNames.WORK_TIME_IN_SECONDS, worktimeInSeconds},
                {EosParameterNames.PUBLIC_KEY, publicKey},
                {EosParameterNames.SECRET_HASH, secretHash},
                {EosParameterNames.PRODUCER_TYPE, producerType}
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

        private Dictionary<string, object> CreateDataForAddVerifyTransactionAndSignature(string chain, string accountName, string blockHash, string verifySignature, byte[] packedTransaction)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.ACCOUNT, accountName},
                {EosParameterNames.BLOCK_HASH, blockHash},
                {EosParameterNames.VERIFY_SIGNATURE, verifySignature},
                {EosParameterNames.PACKED_TRANSACTION, packedTransaction}
            };
        }

        private Dictionary<string, object> CreateDataForApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active")
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.PROPOSER, proposerName },
                { EosParameterNames.PROPOSAL_NAME, proposedTransactionName },
                { EosParameterNames.PERMISSION_LEVEL, new PermissionLevel(){
                    actor = accountName,
                    permission = permission
                }},
                { EosParameterNames.PROPOSAL_HASH, proposalHash }
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
                { EosParameterNames.SIDECHAIN, owner },
                { EosParameterNames.CLAIMER, claimer }
            };
        }

        private Dictionary<string, object> CreateDataForBlackListProd(string owner, string producer)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, producer }
            };
        }

        private Dictionary<string, object> CreateDataForProdPunish(string owner)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.CONTRACT, NetworkConfigurations.BlockBaseOperationsContract }
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


        private Dictionary<string, object> CreateDataForConfigurations(string owner, Dictionary<string, object> contractInformation, List<string> reservedSeats)
        {
            reservedSeats = reservedSeats ?? new List<string>();
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.CONFIG_INFO_JSON, contractInformation },
                { EosParameterNames.RESERVED_SEATS, reservedSeats}
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

        private Dictionary<string, object> CreateDataForLinkAuthorization(string owner, string action, string requirement)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.ACCOUNT, owner },
                { EosParameterNames.CODE, NetworkConfigurations.BlockBaseOperationsContract },
                { EosParameterNames.TYPE, action },
                { EosParameterNames.REQUIREMENT, requirement },
            };
        }

        private Dictionary<string, object> CreateDataForRequestHistoryValidation(string owner, string producerName, string blockHash)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, producerName },
                { EosParameterNames.BLOCK_HASH, blockHash }
            };
        }

        private Dictionary<string, object> CreateDataForValidateHistory(string owner, string producerName)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, producerName }
            };
        }

        private Dictionary<string, object> CreateDataForAddBlockByte(string owner, string producerName, string byteInHexadecimal)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, producerName },
                { EosParameterNames.BYTE_IN_HEXADECIMAL, byteInHexadecimal }
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
                        $"Error sending transaction: {apiException.error.name} Trace: {exception}" :
                        $"Error sending transaction: {exception}";

            _logger.LogCritical(errorMessage);

            throw exception;
        }

        #endregion
    }
}