using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using System.Net;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BlockBase.DataProxy.Encryption;
using System.IO;
using System.Text;
using BlockBase.Node.Commands.Utils;
using Microsoft.AspNetCore.Mvc;
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