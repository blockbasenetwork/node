using System;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Logging;
using BlockBase.DataProxy.Encryption;
using BlockBase.Node.Commands.Utils;
using System.Collections.Generic;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Sql;
using BlockBase.Network.Mainchain;
using BlockBase.Domain.Requests;
using BlockBase.Domain.Configurations;
using BlockBase.Utils.Crypto;
using System.Text;

namespace BlockBase.Node.Commands.Requester
{
    public class ExecuteQueryCommand : AbstractCommand
    {

        private ILogger _logger;
        private DatabaseKeyManager _databaseKeyManager;
        private ExecuteQueryRequest _queryRequest;
        private SqlCommandManager _sqlCommandManager;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;

        public override string CommandName => "Execute query";

        public override string CommandInfo => "Executes the specified query";

        public override string CommandUsage => "run query '<queryToRun>'";

        public ExecuteQueryCommand(ILogger logger, IMainchainService mainchainService, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager, NodeConfigurations nodeConfigurations)
        {
            _databaseKeyManager = databaseKeyManager;
            _sqlCommandManager = sqlCommandManager;
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        public ExecuteQueryCommand(ILogger logger, IMainchainService mainchainService, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager, NodeConfigurations nodeConfigurations, ExecuteQueryRequest queryRequest) : this(logger, mainchainService, databaseKeyManager, sqlCommandManager, nodeConfigurations)
        {
            _queryRequest = queryRequest;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                // var accountPermissions = await _mainchainService.RetrieveAccountPermissions(_nodeConfigurations.AccountName);

                // var accountPublicKey = accountPermissions.FirstOrDefault(a => a.Key == _queryRequest.Account)?.PublicKey;
                // if (accountPublicKey == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Permission for this account not found"));

                // var isSignatureValid = SignatureHelper.VerifySignature(accountPublicKey, _queryRequest.Signature, Encoding.UTF8.GetBytes(_queryRequest.Query));
                // if (!isSignatureValid) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Signature not valid"));

                if (!_databaseKeyManager.DataSynced) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Passwords and main key not set."));
                
                var queryResults = await _sqlCommandManager.Execute(_queryRequest.Query);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 3;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.ToLower().StartsWith("run query");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 5)
            {
                var queryRequest = new ExecuteQueryRequest();
                queryRequest.Query = commandData[2];
                queryRequest.Account = commandData[3];
                queryRequest.Signature = commandData[4];
                return new CommandParseResult(true, true);
            }
            return new CommandParseResult(true, CommandUsage);
        }

    }
}