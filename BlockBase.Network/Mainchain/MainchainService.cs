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
using Newtonsoft.Json;
using Cryptography.ECDSA;
using System.Globalization;

namespace BlockBase.Network.Mainchain
{
    public class MainchainService : IMainchainService
    {
        private EosStub EosStub;
        private NetworkConfigurations NetworkConfigurations;
        private NodeConfigurations NodeConfigurations;
        private readonly ILogger _logger;
        private const int MAX_NUMBER_OF_TRIES = 5;
        private const int TRANSACTION_EXPIRATION = 20;

        public MainchainService(IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations, ILogger<MainchainService> logger)
        {
            NodeConfigurations = nodeConfigurations.Value;
            NetworkConfigurations = networkConfigurations.Value;

            _logger = logger;
            EosStub = new EosStub(TRANSACTION_EXPIRATION, NodeConfigurations.ActivePrivateKey, NetworkConfigurations.EosNetworks);
        }

        public async Task<GetInfoResponse> GetInfo()
        {
            var opResult = await TryAgain(async () => await EosStub.GetInfo(),
            NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<GetBlockResponse> GetEosBlock(string blockNumberOrId)
        {
            var opResult = await TryAgain(async () => await EosStub.GetBlock(blockNumberOrId), NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<string>> GetCurrencyBalance(string smartContractName, string accountName, string symbol = null)
        {
            var opResult = await TryAgain(async () => await EosStub.GetCurrencyBalance(smartContractName, accountName, symbol),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<GetAccountResponse> GetAccount(string accountName)
        {
            var opResult = await TryAgain(async () => await EosStub.GetAccount(accountName),
            NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<TokenLedgerTable>> RetrieveAccountStakedSidechains(string accountName)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_LEDGER_TABLE_NAME, accountName), MAX_NUMBER_OF_TRIES);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.Where(b => b.Owner == accountName).ToList();
        }

        public async Task<AccountStake> GetAccountStake(string sidechain, string accountName)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(
                NetworkConfigurations.BlockBaseTokenContract,
                EosTableNames.TOKEN_LEDGER_TABLE_NAME,
                accountName),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            var stakeInTable = opResult.Result.Where(b => b.Sidechain == sidechain && b.Owner == accountName).FirstOrDefault();
            if (stakeInTable == null) return null;

            decimal stake = 0;

            var stakeString = stakeInTable.Stake?.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            decimal.TryParse(stakeString, NumberStyles.Any, CultureInfo.InvariantCulture, out stake);

            return new AccountStake()
            {
                Sidechain = stakeInTable.Sidechain,
                Owner = stakeInTable.Owner,
                StakeString = stakeInTable.Stake,
                Stake = stake
            };
        }

        #region Transactions

        public async Task<string> AddStake(string sidechain, string accountName, string stake)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
               EosMethodNames.ADD_STAKE,
               NetworkConfigurations.BlockBaseTokenContract,
               accountName,
               CreateDataForAddStake(sidechain, accountName, stake)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ClaimStake(string sidechain, string accountName)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
               EosMethodNames.CLAIM_STAKE,
               NetworkConfigurations.BlockBaseTokenContract,
               accountName,
               CreateDataForClaimStake(sidechain, accountName)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddCandidature(string chain, string accountName, string publicKey, string secretHash, int producerType, int softwareVersion)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_CANDIDATE,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddCandidate(chain, accountName, publicKey, secretHash, producerType, softwareVersion)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> RemoveCandidature(string chain, string accountName)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.REMOVE_CANDIDATE,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForRemoveCandidate(chain, accountName)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddSecret(string chain, string accountName, string hash)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_SECRET,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddSecret(chain, accountName, hash)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddBlock(string chain, string accountName, Dictionary<string, object> blockHeader)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_BLOCK,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddBlock(chain, accountName, blockHeader)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddEncryptedIps(string chain, string accountName, List<string> encryptedIps)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_ENCRYPTED_IP,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddEncryptedIps(chain, accountName, encryptedIps)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> UpdatePublicKey(string chain, string accountName, string publicKey)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.UPDATE_KEY,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForUpdatePublicKey(chain, accountName, publicKey)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddReservedSeats(string chain, List<Dictionary<string, object>> seatsToAdd)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_RESERVED_SEATS,
                NetworkConfigurations.BlockBaseOperationsContract,
                chain,
                CreateDataForAddReservedSeats(chain, seatsToAdd)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> RemoveReservedSeats(string chain, List<string> reservedSeatsToRemove)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.REMOVE_RESERVED_SEATS,
                NetworkConfigurations.BlockBaseOperationsContract,
                chain,
                CreateDataForRemoveReservedSeats(chain, reservedSeatsToRemove)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> NotifyReady(string chain, string accountName)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.I_AM_READY,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForIAmReady(chain, accountName)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> VerifyBlock(string chain, string producer, string blockHash)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.VERIFY_BLOCK,
                NetworkConfigurations.BlockBaseOperationsContract,
                chain,
                CreateDataForVerifyBlock(chain, producer, blockHash)),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ProposeBlockVerification(string chain, string accountName, List<string> requestedApprovals, string blockHash)
        {
            var opResult = await TryAgain(async () => await EosStub.ProposeTransaction(
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
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ApproveTransaction(string proposerName, string proposedTransactionName, string accountName, string proposalHash, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_APPROVE_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                accountName,
                CreateDataForApproveTransaction(proposerName, proposedTransactionName, accountName, proposalHash),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ExecuteTransaction(string proposerName, string proposedTransactionName, string accountName, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_EXEC_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                accountName,
                CreateDataForExecTransaction(proposerName, proposedTransactionName, accountName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }


        public async Task<string> CancelTransaction(string proposerName, string proposedTransactionName, string cancelerName = null, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMsigConstants.EOSIO_MSIG_CANCEL_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                cancelerName ?? proposerName,
                CreateDataForCancelTransaction(proposerName, proposedTransactionName, cancelerName ?? proposerName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ClaimReward(string chain, string producerName, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.CLAIM_REWARD,
                NetworkConfigurations.BlockBaseTokenContract,
                producerName,
                CreateDataForClaimReward(chain, producerName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> StartChain(string owner, string publicKey, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.START_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForStartChain(owner, publicKey),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ConfigureChain(string owner, Dictionary<string, object> contractInformation, List<Dictionary<string, object>> reservedSeats = null, int minimumSoftwareVersion = 1, Dictionary<string, object> blockHeaderToInitialize = null, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.CONFIG_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForConfigurations(owner, contractInformation, reservedSeats, minimumSoftwareVersion, blockHeaderToInitialize),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AlterConfigurations(string owner, Dictionary<string, object> configurationsToChange, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ALTER_CONFIG,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForAlterConfigurations(owner, configurationsToChange),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> EndChain(string owner, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.END_CHAIN,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                //TODO rpinto - is this still a deferred transaction - doesn't seem so
                CreateDataForTransaction(owner),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> StartCandidatureTime(string owner, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.START_CANDIDATURE_TIME,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForTransaction(owner),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> SidechainExitRequest(string sidechainName, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.EXIT_REQUEST,
                NetworkConfigurations.BlockBaseOperationsContract,
                NodeConfigurations.AccountName,
                CreateDataForSidechainExitRequest(sidechainName),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> BlacklistProducer(string owner, string producerToBlacklist, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.BLACKLIST_PRODUCERS,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForBlackListProd(owner, producerToBlacklist),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> RemoveBlacklistedProducer(string owner, string producerToRemove, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.REMOVE_BLACKLISTED,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForBlackListProd(owner, producerToRemove),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> PunishProd(string owner, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.PUNISH_PRODUCERS,
                NetworkConfigurations.BlockBaseTokenContract,
                owner,
                CreateDataForProdPunish(owner),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<int> ExecuteChainMaintainerAction(string actionname, string accountname, string permission = "active")
        {
            var timeBeforeSend = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                actionname,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountname,
                CreateDataForTransaction(accountname),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries, null,
                0
            );

            if (!opResult.Succeeded) throw opResult.Exception;

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

            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.UPDATEAUTH,
                EosAtributeNames.EOSIO,
                accountname,
                CreateDataForUpdateAuthorization(accountname, authorizationToAssign, permission, newAutorization),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> LinkAuthorization(string actionName, string accountname, string authorization, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.LINKAUTH,
                EosAtributeNames.EOSIO,
                accountname,
                CreateDataForLinkAuthorization(accountname, actionName, authorization),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> RequestHistoryValidation(string owner, string producerName, string blockHash, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.REQUEST_HISTORY_VALIDATION,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForRequestHistoryValidation(owner, producerName, blockHash),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
                );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> SubmitBlockByte(string owner, string producerName, string byteInHexadecimal, string blockHash, string permission = "active")
        {
            var transaction = new Transaction()
            {
                expiration = DateTime.UtcNow.AddHours(1),
                actions = new List<EosSharp.Core.Api.v1.Action>()
                {
                    new EosSharp.Core.Api.v1.Action()
                    {
                        account = NetworkConfigurations.BlockBaseOperationsContract,
                        authorization = new List<PermissionLevel>()
                        {
                            new PermissionLevel() {actor = owner, permission = EosMsigConstants.VERIFY_HISTORY_PERMISSION }
                        },
                        name = EosMethodNames.HISTORY_VALIDATE,
                        data = CreateDataForHistValidate(owner, producerName, blockHash)
                    }
                }
            };

            var privateKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(NodeConfigurations.ActivePrivateKey);
            var publicKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privateKeyBytes, true));
            var signedTransaction = await EosStub.SignTransaction(transaction, publicKey);
            return await AddBlockByteVerifyTransactionAndSignature(owner, producerName, byteInHexadecimal, signedTransaction.PackedTransaction);

        }

        public async Task<string> AddBlockByteVerifyTransactionAndSignature(string owner, string accountName, string byteInHexadecimal, byte[] packedTransaction, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                    EosMethodNames.ADD_BLOCK_BYTE,
                    NetworkConfigurations.BlockBaseOperationsContract,
                    accountName,
                    CreateDataForAddBlockByte(owner, accountName, byteInHexadecimal, packedTransaction),
                    permission),
                    NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;

        }

        public async Task<string> SignHistoryValidation(string owner, string accountName, string producerToValidade, string byteInHexadecimal, Transaction transaction, string permission = "active")
        {
            var privateKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(NodeConfigurations.ActivePrivateKey);
            var publicKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privateKeyBytes, true));
            var signedTransaction = await EosStub.SignTransaction(transaction, publicKey);

            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                   EosMethodNames.ADD_HIST_SIG,
                   NetworkConfigurations.BlockBaseOperationsContract,
                   accountName,
                   CreateDataForSignHistoryValidation(owner, accountName, producerToValidade, byteInHexadecimal, signedTransaction.Signatures.SingleOrDefault(), signedTransaction.PackedTransaction),
                   permission),
                   NetworkConfigurations.MaxNumberOfConnectionRetries
           );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> ProposeHistoryValidation(string owner, string accountName, List<string> requestedApprovals, string proposalName)
        {
            var opResult = await TryAgain(async () => await EosStub.ProposeTransaction(
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

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> CreateVerifyBlockTransactionAndAddToContract(string owner, string accountName, string blockHash)
        {
            var transaction = new Transaction()
            {
                expiration = DateTime.UtcNow.AddHours(1),
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

            var privateKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(NodeConfigurations.ActivePrivateKey);
            var publicKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privateKeyBytes, true));
            var signedTransaction = await EosStub.SignTransaction(transaction, publicKey);
            return await AddVerifyTransactionAndSignature(owner, accountName, blockHash, signedTransaction.Signatures.FirstOrDefault(), signedTransaction.PackedTransaction);
        }

        public async Task<string> SignVerifyTransactionAndAddToContract(string owner, string account, string blockHash, Transaction transaction, string permission = "active")
        {
            var privateKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(NodeConfigurations.ActivePrivateKey);
            var publicKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privateKeyBytes, true));
            var signedTransaction = await EosStub.SignTransaction(transaction, publicKey);
            return await AddVerifyTransactionAndSignature(owner, account, blockHash, signedTransaction.Signatures.FirstOrDefault(), signedTransaction.PackedTransaction);
        }

        public async Task<string> BroadcastTransactionWithSignatures(byte[] packedTransaction, List<string> signatures)
        {
            var signedTransaction = new SignedTransaction()
            {
                PackedTransaction = packedTransaction,
                Signatures = signatures.Distinct()
            };

            var opResult = await TryAgain(async () =>
            await EosStub.BroadcastTransaction(signedTransaction), NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddVerifyTransactionAndSignature(string owner, string accountName, string blockHash, string verifySignature, byte[] verifyBlockTransaction, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_VERIFY_SIGNATURE,
                NetworkConfigurations.BlockBaseOperationsContract,
                accountName,
                CreateDataForAddVerifyTransactionAndSignature(owner, accountName, blockHash, verifySignature, verifyBlockTransaction),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> UnlinkAction(string owner, string actionToUnlink, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.UNLINK_AUTH,
                EosAtributeNames.EOSIO,
                owner,
                CreateDataForUnlinkAuthorization(owner, actionToUnlink),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> DeletePermission(string owner, string permissionToDelete, string permission = "active")
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.DELETE_AUTH,
                EosAtributeNames.EOSIO,
                owner,
                CreateDataForDeleteAuthorization(owner, permissionToDelete),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> AddAccountPermission(string owner, string accountToAdd, string accountPublicKey, string permissions, string permission)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.ADD_ACCOUNT_PERMISSION,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForAddAccountPermission(owner, accountToAdd, accountPublicKey, permissions),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<string> RemoveAccountPermission(string owner, string accountToRemove, string permission)
        {
            var opResult = await TryAgain(async () => await EosStub.SendTransaction(
                EosMethodNames.REMOVE_ACCOUNT_PERMISSION,
                NetworkConfigurations.BlockBaseOperationsContract,
                owner,
                CreateDataForRemoveAccountPermission(owner, accountToRemove),
                permission),
                NetworkConfigurations.MaxNumberOfConnectionRetries
            );
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        #endregion

        #region Table Retrievers

        public async Task<List<ProducerInTable>> RetrieveProducersFromTable(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.PRODUCERS_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<CurrentProducerTable> RetrieveCurrentProducer(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CurrentProducerTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.CURRENT_PRODUCER_TABLE_NAME, chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.SingleOrDefault();
        }


        public async Task<List<CandidateTable>> RetrieveCandidates(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CandidateTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.CANDIDATES_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<BlockheaderTable>> RetrieveBlockheaderList(string chain, int numberOfBlocks)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.BLOCKHEADERS_TABLE_NAME,
                chain,
                numberOfBlocks,
                true),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<IPAddressTable>> RetrieveIPAddresses(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<IPAddressTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.IP_ADDRESS_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<BlockCountTable>> RetrieveBlockCount(string proposerAccount)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockCountTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.BLOCKCOUNT_TABLE_NAME,
                proposerAccount),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;

        }

        public async Task<List<RewardTable>> RetrieveRewardTable(string account)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<RewardTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.PENDING_REWARD_TABLE,
                account),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<ReservedSeatsTable>> RetrieveReservedSeatsTable(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ReservedSeatsTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.RESERVED_SEATS_TABLE,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<TransactionProposalApprovalsTable> RetrieveApprovals(string proposerAccount, string proposalName)
        {
            var opResult = await TryAgain(async () => (await EosStub.GetRowsFromSmartContractTable<TransactionProposalApprovalsTable>(
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                EosMsigConstants.EOSIO_MSIG_APPROVALS_TABLE_NAME,
                proposerAccount)),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.Where(t => t.ProposalName == proposalName).SingleOrDefault();

        }

        public async Task<ContractInformationTable> RetrieveContractInformation(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractInformationTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.CONTRACT_INFO_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.SingleOrDefault();
        }

        public async Task<VersionTable> RetrieveSidechainNodeVersion(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<VersionTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.VERSION_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.SingleOrDefault();
        }

        public async Task<ContractStateTable> RetrieveContractState(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractStateTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.CONTRACT_STATE_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.SingleOrDefault();
        }

        public async Task<BlockheaderTable> RetrieveLastBlockFromLastSettlement(string chain, int numberOfBlocks)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.BLOCKHEADERS_TABLE_NAME,
                chain,
                numberOfBlocks,
                true),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.Where(b => b.IsLastBlock == true).FirstOrDefault();
        }

        public async Task<ClientTable> RetrieveClientTable(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ClientTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.CLIENT_TABLE_NAME,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.SingleOrDefault();
        }

        public async Task<BlockheaderTable> GetLastSubmittedBlockheader(string chain, int numberOfBlocks)
        {
            var lastSubmittedBlock = (await RetrieveBlockheaderList(chain, numberOfBlocks)).FirstOrDefault();

            return lastSubmittedBlock;
        }

        public async Task<BlockheaderTable> GetLastValidSubmittedBlockheader(string chain, int numberOfBlocks)
        {
            var lastValidSubmittedBlock = (await RetrieveBlockheaderList(chain, numberOfBlocks)).Where(b => b.IsVerified).FirstOrDefault();

            return lastValidSubmittedBlock;
        }

        public async Task<BlockheaderTable> GetLastIrreversibleBlockHeader(string chain, int numberOfBlocks)
        {
            var blockList = await RetrieveBlockheaderList(chain, numberOfBlocks);
            var networkInfo = await GetInfo();
            var lastIrreversibleEosBlock = await GetEosBlock(networkInfo.last_irreversible_block_id);

            var irreversibleBlocks = blockList.Where(b => b.Timestamp < ((DateTimeOffset)lastIrreversibleEosBlock.timestamp).ToUnixTimeSeconds());
            if (irreversibleBlocks.Count() >= 2) irreversibleBlocks = irreversibleBlocks.Skip(1);
            return irreversibleBlocks.FirstOrDefault();
        }

        public async Task<TransactionProposal> RetrieveProposal(string proposerName, string proposalName)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TransactionProposalTable>(
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                EosMsigConstants.EOSIO_MSIG_PROPOSAL_TABLE_NAME,
                proposerName),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;

            var proposal = opResult.Result?.FirstOrDefault(x => x.ProposalName == proposalName);
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

            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<VerifySignatureTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.VERIFY_SIGNATURE_TABLE,
                account),
                NetworkConfigurations.MaxNumberOfConnectionRetries);

            if (!opResult.Succeeded) throw opResult.Exception;

            foreach (var verifySignature in opResult.Result)
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

        public async Task<IList<MappedHistoryValidation>> RetrieveHistoryValidation(string chain)
        {
            var historyValidationList = new List<MappedHistoryValidation>();

            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<HistoryValidationTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.HISTORY_VALIDATION_TABLE,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;


            foreach (var historyValidation in opResult.Result)
            {
                var mappedHistoryValidation = new MappedHistoryValidation()
                {
                    Account = historyValidation.Key,
                    BlockHash = historyValidation.BlockHash,
                    VerifySignatures = historyValidation.VerifySignatures,
                    SignedProducers = historyValidation.SignedProducers,
                    BlockByteInHexadecimal = historyValidation.BlockByteInHexadecimal,
                    PackedTransaction = SerializationHelper.HexStringToByteArray(historyValidation.ValidateHistoryPackedTransaction),
                    Transaction = string.IsNullOrEmpty(historyValidation.ValidateHistoryPackedTransaction) ? null : await EosStub.UnpackTransaction(historyValidation.ValidateHistoryPackedTransaction)
                };

                historyValidationList.Add(mappedHistoryValidation);
            }

            return historyValidationList;
        }

        public async Task<List<BlackListTable>> RetrieveBlacklistTable(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlackListTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.BLACKLIST_TABLE,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<WarningTable>> RetrieveWarningTable(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<WarningTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.WARNING_TABLE,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<List<AccountPermissionsTable>> RetrieveAccountPermissions(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<AccountPermissionsTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.ACCOUNT_PERMISSIONS_TABLE,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result;
        }

        public async Task<ChangeConfigurationTable> RetrieveConfigurationChanges(string chain)
        {
            var opResult = await TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ChangeConfigurationTable>(
                NetworkConfigurations.BlockBaseOperationsContract,
                EosTableNames.CHANGE_CONFIG_TABLE,
                chain),
                NetworkConfigurations.MaxNumberOfConnectionRetries);
            if (!opResult.Succeeded) throw opResult.Exception;
            return opResult.Result.SingleOrDefault();
        }


        #endregion

        #region Data Helpers

        private Dictionary<string, object> CreateDataForAddStake(string sidechain, string accountName, string stake)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, accountName},
                {EosParameterNames.SIDECHAIN, sidechain},
                {EosParameterNames.STAKE, stake}
            };
        }

        private Dictionary<string, object> CreateDataForClaimStake(string sidechain, string claimer)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.SIDECHAIN, sidechain},
                {EosParameterNames.CLAIMER, claimer}
            };
        }

        private Dictionary<string, object> CreateDataForAddCandidate(string chain, string name, string publicKey, string secretHash, int producerType, int softwareVersion)
        {
            return new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, chain},
                {EosParameterNames.CANDIDATE, name},
                {EosParameterNames.PUBLIC_KEY, publicKey},
                {EosParameterNames.SECRET_HASH, secretHash},
                {EosParameterNames.PRODUCER_TYPE, producerType},
                {EosParameterNames.SOFTWARE_VERSION, softwareVersion}
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

        public Dictionary<string, object> CreateDataForUpdatePublicKey(string chain, string accountName, string publicKey)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, chain },
                { EosParameterNames.PRODUCER, accountName },
                { EosParameterNames.PUBLIC_KEY, publicKey }
            };
        }

        private Dictionary<string, object> CreateDataForAddReservedSeats(string chain, List<Dictionary<string, object>> seatsToAdd)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, chain},
                { EosParameterNames.SEATS_TO_ADD, seatsToAdd }
            };
        }

        private Dictionary<string, object> CreateDataForRemoveReservedSeats(string chain, List<string> reservedSeatsToRemove)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, chain},
                { EosParameterNames.SEATS_TO_REMOVE, reservedSeatsToRemove }
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

        private Dictionary<string, object> CreateDataForTransaction(string owner)
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


        private Dictionary<string, object> CreateDataForConfigurations(string owner, Dictionary<string, object> contractInformation, List<Dictionary<string, object>> reservedSeats, int minimumSoftwareVersion, Dictionary<string, object> blockHeaderToInitialize = null)
        {
            reservedSeats = reservedSeats ?? new List<Dictionary<string, object>>();
            var config = new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.CONFIG_INFO_JSON, contractInformation },
                { EosParameterNames.RESERVED_SEATS, reservedSeats },
                { EosParameterNames.SOFTWARE_VERSION, minimumSoftwareVersion }
            };
            if (blockHeaderToInitialize != null)
            {
                config.Add(EosParameterNames.STARTING_BLOCK, blockHeaderToInitialize);
            }
            return config;
        }

        private Dictionary<string, object> CreateDataForAlterConfigurations(string owner, Dictionary<string, object> contractInformation)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.INFO_CHANGE_JSON, contractInformation },
            };
        }

        private Dictionary<string, object> CreateDataForSidechainExitRequest(string sidechainName)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, sidechainName },
                { EosParameterNames.ACCOUNT, NodeConfigurations.AccountName },
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

        private Dictionary<string, object> CreateDataForDeleteAuthorization(string owner, string permissionToDelete)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.ACCOUNT, owner },
                { EosParameterNames.PERMISSION, permissionToDelete }
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

        private Dictionary<string, object> CreateDataForUnlinkAuthorization(string owner, string action)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.ACCOUNT, owner },
                { EosParameterNames.CODE, NetworkConfigurations.BlockBaseOperationsContract },
                { EosParameterNames.TYPE, action }
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

        private Dictionary<string, object> CreateDataForAddBlockByte(string owner, string producerName, string byteInHexadecimal, byte[] packedTransaction)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, producerName },
                { EosParameterNames.BYTE_IN_HEXADECIMAL, byteInHexadecimal },
                { EosParameterNames.PACKED_TRANSACTION, packedTransaction }

            };
        }
        private Dictionary<string, object> CreateDataForHistValidate(string owner, string producerName, string blockHash)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, producerName },
                { EosParameterNames.BLOCK_HASH, blockHash }
            };
        }



        private Dictionary<string, object> CreateDataForRemoveCandidate(string owner, string producerName)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.NAME, producerName }
            };
        }

        private Dictionary<string, object> CreateDataForSignHistoryValidation(string owner, string accountName, string producerToValidade, string byteInHexadecimal, string signature, byte[] packedTransaction)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.PRODUCER, accountName },
                { EosParameterNames.PRODUCER_TO_VALIDATE, producerToValidade },
                { EosParameterNames.VERIFY_SIGNATURE, signature},
                { EosParameterNames.PACKED_TRANSACTION, packedTransaction }
            };

        }

        private Dictionary<string, object> CreateDataForAddAccountPermission(string owner, string accountToAdd, string accountPublicKey, string permissions)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.ACCOUNT, accountToAdd },
                { EosParameterNames.PUBLIC_KEY, accountPublicKey },
                { EosParameterNames.PERMISSIONS, permissions}
            };
        }

        private Dictionary<string, object> CreateDataForRemoveAccountPermission(string owner, string accountToRemove)
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.OWNER, owner },
                { EosParameterNames.ACCOUNT, accountToRemove }
            };
        }


        #endregion

        #region Transaction Send Helper

        public async Task<OpResult<T>> TryAgain<T>(Func<Task<OpResult<T>>> func, int maxTry = 15, Func<OpResult<T>, bool> expectedResult = null, int delayInMilliseconds = 100)
        {
            var i = maxTry;
            return await TryAgain(func, () => i-- >= 0, expectedResult, delayInMilliseconds);
        }

        public async Task<OpResult<T>> TryAgain<T>(Func<Task<OpResult<T>>> func, Func<bool> goodUntil, Func<OpResult<T>, bool> expectedResult = null, int delayInMilliseconds = 100)
        {
            Exception exception = null;

            while (goodUntil())
            {
                var opResult = await func.Invoke();

                if (opResult.Succeeded && expectedResult == null)
                {
                    return opResult;
                }
                else if (opResult.Succeeded && expectedResult != null && expectedResult.Invoke(opResult))
                {
                    return opResult;
                }
                else if (opResult.Succeeded && expectedResult != null && !expectedResult.Invoke(opResult))
                {
                    exception = new ArgumentException($"Expected result not found");
                }
                else
                {
                    exception = opResult.Exception;
                }

                if (exception is ApiErrorException)
                {
                    var apiEx = (ApiErrorException)exception;
                    var details = apiEx.error?.details;
                    if (details != null && details.Any(d => d.method == "eosio_assert" || d.method == "apply_eosio_linkauth" || d.method == "tx_duplicate" || d.method == "unsatisfied_authorization" || d.method == "check_authorization" || d.method == "handle_exception"))
                    {
                        //if it's a message that we may be expecting do a quieter log and stop the loop
                        _logger.LogDebug($"Error sending transaction to Api {EosStub.GetCurentNetwork()}: {apiEx.error.name} Message: {apiEx.error.details.FirstOrDefault()?.message}");
                        return new OpResult<T>(exception);
                    }
                    else
                    {
                        EosStub.ChangeNetwork();
                    }
                }

                //Will try to change network if the current one isn't able to respond to the requested endpoint
                if (!(opResult.Exception is ApiErrorException))
                {
                    EosStub.ChangeNetwork();
                }

                await Task.Delay(delayInMilliseconds);
            }

            var errorMessage = exception is ApiErrorException apiException ?
                        $"Error sending transaction to Api {EosStub.GetCurentNetwork()}: {apiException.error.name} Message: {apiException.error.details.FirstOrDefault()?.message}" :
                        $"Error sending transaction to Api {EosStub.GetCurentNetwork()}";

            _logger.LogCritical(errorMessage);
            //_logger.LogDebug($"Send Transaction Error Trace: {exception}");

            return new OpResult<T>(exception);
        }

        public void ChangeNetwork() => EosStub.ChangeNetwork();

        #endregion
    }
}