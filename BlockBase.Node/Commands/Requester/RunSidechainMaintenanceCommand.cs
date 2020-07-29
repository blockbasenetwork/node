using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Requester;

namespace BlockBase.Node.Commands.Requester
{
    public class RunSidechainMaintenanceCommand : AbstractCommand
    {

        private ILogger _logger;
        private ISidechainMaintainerManager _sidechainMaintainerManager;


        public override string CommandName => "Run sidechain maintenance";

        public override string CommandInfo => "Activates update sidechain status";

        public override string CommandUsage => "run sidechain";

        public RunSidechainMaintenanceCommand(ILogger logger, ISidechainMaintainerManager sidechainMaintainerManager)
        {
            _logger = logger;
            _sidechainMaintainerManager = sidechainMaintainerManager;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (_sidechainMaintainerManager.IsMaintainerRunning() || _sidechainMaintainerManager.IsProductionRunning())
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain was already running."));
                }

                await _sidechainMaintainerManager.Start();
                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, "Chain maintenance started."));
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