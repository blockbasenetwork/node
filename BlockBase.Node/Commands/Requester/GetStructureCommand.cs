using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.Node.Commands.Utils;
using BlockBase.DataProxy.Encryption;
using System.Collections.Generic;
using BlockBase.Runtime.Sql;
using BlockBase.Domain.Pocos;

namespace BlockBase.Node.Commands.Requester
{
    public class GetStructureCommand : AbstractCommand
    {

        private ILogger _logger;
        private DatabaseKeyManager _databaseKeyManager;
        private SqlCommandManager _sqlCommandManager;
        

        public override string CommandName => "Get databases structure";

        public override string CommandInfo => "Gets databases, tables and columns of requester";

        public override string CommandUsage => "get struct";

        public GetStructureCommand(ILogger logger, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager)
        {
            _logger = logger;
            _databaseKeyManager = databaseKeyManager;
            _sqlCommandManager = sqlCommandManager;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (!_databaseKeyManager.DataSynced) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Passwords and main key not set."));
                var structure = _sqlCommandManager.GetStructure();
                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<IList<DatabasePoco>>(structure));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.ToLower().StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            return new CommandParseResult(true, true);
        }

    }
}