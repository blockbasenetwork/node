using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Utils.Operation;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Helpers;
using EosSharp.Core.Providers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Network.Mainchain
{
    public class EosStub
    {
        private uint _transactionExpirationTimeInSeconds;
        private Eos _eosConnection;
        private EosConfigurator _eosConfig;
        private readonly string _privateKey;

        public EosStub()
        {
        }

        public EosStub(uint transactionExpirationTimeInSeconds, string privateKey, string network)
        {
            _transactionExpirationTimeInSeconds = transactionExpirationTimeInSeconds;
            _privateKey = privateKey;
            _eosConfig = SetConfiguration(network);
            _eosConnection = OpenConnectionWithChain();
        }

        private Eos OpenConnectionWithChain()
        {
            return _eosConnection = new Eos(_eosConfig);
        }

        private EosConfigurator SetConfiguration(string network)
        {
            return _eosConfig = new EosConfigurator()
            {
                HttpEndpoint = network,
                ExpireSeconds = _transactionExpirationTimeInSeconds,
                SignProvider = new DefaultSignProvider(_privateKey)
            };
        }

        public async Task<OpResult<List<string>>> GetCurrencyBalance(string smartContractName, string accountName, string symbol = null) 
        {
            return await Op.RunAsync(async () => await _eosConnection.GetCurrencyBalance(smartContractName, accountName, symbol));
        }

        public async Task<OpResult<string>> SendTransaction(string actionName, string smartContractAccountName, string accountName, Dictionary<string, object> data, string permission = "active", byte cpuLimit = default(byte))
        {
            var opResult = await Op.RunAsync(async () => await _eosConnection.CreateTransaction(new Transaction()
            {
                actions = new List<EosSharp.Core.Api.v1.Action>()
                    {
                        new EosSharp.Core.Api.v1.Action()
                        {
                            account = smartContractAccountName,
                            authorization = new List<PermissionLevel>()
                            {
                                new PermissionLevel() {actor = accountName, permission = permission }
                            },
                            name = actionName,
                            data = data
                        }
                    },
                max_cpu_usage_ms = cpuLimit
            }));
            return opResult;
        }

        public async Task<OpResult<List<TPoco>>> GetRowsFromSmartContractTable<TPoco>(string smartContractAccountName, string tableName, string scope = null, int limit = 100)
        {
            var opResult = await Op.RunAsync(async () => (await _eosConnection.GetTableRows<TPoco>(
                new GetTableRowsRequest()
                {
                    json = true,
                    code = smartContractAccountName,
                    scope = scope ?? smartContractAccountName,
                    table = tableName,
                    limit = limit
                }
            )).rows);

            return opResult;
        }

        public async Task<OpResult<GetAccountResponse>> GetAccount(string accountName)
            => await Op.RunAsync(async () => (await _eosConnection.GetAccount(accountName)));

        public async Task<OpResult<GetInfoResponse>> GetInfo()
            => await Op.RunAsync(async () => (await _eosConnection.GetInfo()));

        #region Multi Signature Transactions

        public async Task<OpResult<string>> ProposeTransaction(string actionName, string smartContractAccountName, string signerAccountName, string proposerAccountName, object data, List<string> requestedApprovals, string proposalName, string proposePermission = "active", string permission = "active") =>
            await SendTransaction(
                EosMsigConstants.EOSIO_MSIG_PROPOSE_ACTION,
                EosMsigConstants.EOSIO_MSIG_ACCOUNT_NAME,
                proposerAccountName,
                CreateDataForProposeTransaction(
                    proposerAccountName,
                    requestedApprovals,
                    await CreateProposedTransaction(actionName, smartContractAccountName, signerAccountName, proposerAccountName, data, proposePermission),
                    proposalName),
                permission
            );

        public async Task<SignedTransaction> SignTransaction(Transaction trx, string keyToUse)
        {
            return await _eosConnection.SignTransaction(trx, new List<string>(){keyToUse});
        }

        public async Task<OpResult<string>> BroadcastTransaction(SignedTransaction trx)
        {
            var opResult = await Op.RunAsync(async () => await _eosConnection.BroadcastTransaction(trx));
            return opResult;
        }

        #endregion

        #region Transaction Helpers

        //TODO: Review method, see if there is a better way to serialize data to byte array
        private async Task<Transaction> CreateProposedTransaction(string actionName, string smartContractAccountName, string signerAccountName, string proposerAccountName, object data, string permission = "active")
        {
            var eosApi = new EosApi(_eosConfig, new HttpHandler());
            var abiSerializer = new EosSharp.Core.Providers.AbiSerializationProvider(eosApi);

            var transaction = new Transaction()
            {
                expiration = DateTime.UtcNow.AddDays(1),
                actions = new List<EosSharp.Core.Api.v1.Action>()
                {
                    new EosSharp.Core.Api.v1.Action()
                    {
                        account = smartContractAccountName,
                        authorization = new List<PermissionLevel>()
                        {
                            new PermissionLevel() {actor = signerAccountName, permission = permission }
                        },
                        name = actionName,
                        data = data
                    }
                }
            };

            var abiResponses = await abiSerializer.GetTransactionAbis(transaction);
            int actionIndex = 0;

            foreach (var action in transaction.actions)
            {
                action.data = abiSerializer.SerializeActionData(action, abiResponses[actionIndex++]);
            }

            return transaction;
        }

        public async Task<byte[]> PackTransaction(Transaction transaction)
        {
            var eosApi = new EosApi(_eosConfig, new HttpHandler());
            var abiSerializer = new AbiSerializationProvider(eosApi);

            return await abiSerializer.SerializePackedTransaction(transaction);
        }

        public async Task<Transaction> UnpackTransaction(string packedTransaction)
        {
            var eosApi = new EosApi(_eosConfig, new HttpHandler());
            var abiSerializer = new AbiSerializationProvider(eosApi);

            return await abiSerializer.DeserializePackedTransaction(packedTransaction);
        }

        #endregion

        #region Create Data Helpers

        private Dictionary<string, object> CreateDataForProposeTransaction(string proposer, List<string> requestedAccounts, Transaction proposedTransaction, string proposalName, string permission = "active")
        {
            var requested = new List<PermissionLevel>();

            foreach (var requestedAccount in requestedAccounts)
            {
                requested.Add(new PermissionLevel()
                {
                    actor = requestedAccount,
                    permission = permission
                });
            }

            return new Dictionary<string, object>()
            {
                { EosParameterNames.PROPOSER, proposer },
                { EosParameterNames.PROPOSAL_NAME, proposalName },
                { EosParameterNames.REQUESTED_PERMISSIONS, requested},
                { EosParameterNames.TRANSACTION, proposedTransaction}
            };
        }

        #endregion
    }
}