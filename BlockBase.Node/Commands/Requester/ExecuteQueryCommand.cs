using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.DataProxy.Encryption;
using BlockBase.Node.Commands.Utils;
using System.Collections.Generic;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Sql;

namespace BlockBase.Node.Commands.Requester
{
    public class ExecuteQueryCommand : AbstractCommand
    {

        private ILogger _logger;
        private DatabaseKeyManager _databaseKeyManager;
        private string _queryScript;
        private SqlCommandManager _sqlCommandManager;


        public override string CommandName => "Execute query";

        public override string CommandInfo => "Executes the specified query";

        public override string CommandUsage => "run query '<queryToRun>'";

        public ExecuteQueryCommand(ILogger logger, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager)
        {
            _databaseKeyManager = databaseKeyManager;
            _sqlCommandManager = sqlCommandManager;
            _logger = logger;
        }

        public ExecuteQueryCommand(ILogger logger, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager, string queryScript) : this(logger, databaseKeyManager, sqlCommandManager)
        {
            _queryScript = queryScript;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (!_databaseKeyManager.DataSynced) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Passwords and main key not set."));
                var queryResults = await _sqlCommandManager.Execute(_queryScript);

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
            if (commandData.Length == 3)
            {
                _queryScript = commandData[2];
                return new CommandParseResult(true, true);
            }
            return new CommandParseResult(true, CommandUsage);
        }

    }
}