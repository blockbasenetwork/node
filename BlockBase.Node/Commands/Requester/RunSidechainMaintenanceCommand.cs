using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Requester;
using BlockBase.Network.Mainchain;
using BlockBase.Domain.Configurations;

namespace BlockBase.Node.Commands.Requester
{
    public class RunSidechainMaintenanceCommand : AbstractCommand
    {

        private ILogger _logger;
        private ISidechainMaintainerManager _sidechainMaintainerManager;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;


        public override string CommandName => "Run sidechain maintenance";
        public override string CommandInfo => "Activates update sidechain status";
        public override string CommandUsage => "run sidechain";

        public RunSidechainMaintenanceCommand(ILogger logger, ISidechainMaintainerManager sidechainMaintainerManager, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _logger = logger;
            _sidechainMaintainerManager = sidechainMaintainerManager;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var sidechainInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);

                if (sidechainInfo == null || sidechainInfo.Key != _nodeConfigurations.AccountName)
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain not started and configured yet."));
                }
                
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