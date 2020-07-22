using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Requester;
using BlockBase.DataProxy.Encryption;
using System.Collections.Generic;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Sql;
using BlockBase.Domain.Pocos;

namespace BlockBase.Node.Commands.Requester
{
    public class RemoveSidechainDatabasesAndKeysCommand : AbstractCommand
    {

        private ILogger _logger;
        private SqlCommandManager _sqlCommandManager;
        ISidechainMaintainerManager _sidechainMaintainerManager;


        public override string CommandName => "Remove sidechain databases and keys";

        public override string CommandInfo => "Removes all sidechain associated databases and keys";

        public override string CommandUsage => "remove data";

        public RemoveSidechainDatabasesAndKeysCommand(ILogger logger, ISidechainMaintainerManager sidechainMaintainerManager, SqlCommandManager sqlCommandManager)
        {
            _logger = logger;
            _sqlCommandManager = sqlCommandManager;
            _sidechainMaintainerManager = sidechainMaintainerManager;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (_sidechainMaintainerManager.IsMaintainerRunning() || _sidechainMaintainerManager.IsProductionRunning())
                    return new CommandExecutionResponse( HttpStatusCode.BadRequest, new OperationResponse(false, "The sidechain maintenance is running."));
                await _sqlCommandManager.RemoveSidechainDatabasesAndKeys();
                return new CommandExecutionResponse( HttpStatusCode.OK, new OperationResponse(true, $"Deleted databases and cleared all data."));
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