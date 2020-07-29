using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Requester
{
    public class ClaimStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;

        
        public override string CommandName => "Claim stake";

        public override string CommandInfo => "Claims sidechain leftover stake";

        public override string CommandUsage => "claim stake";

        public ClaimStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            
           try
            {
                var trx = await _mainchainService.ClaimStake(_nodeConfigurations.AccountName, _nodeConfigurations.AccountName);

                return new CommandExecutionResponse( HttpStatusCode.OK, new OperationResponse(true, $"Stake successfully claimed. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            } }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2 || commandData.Length == 4;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("request sidechain");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);
            if (commandData.Length == 4)
            {
                if (commandData[2] != "--stake") return new CommandParseResult(true, CommandUsage);
                if (!decimal.TryParse(commandData[3], out var stake)) return new CommandParseResult(true, "Unable to parse stake");
                Stake = stake;
                return new CommandParseResult(true, true);
            }
            
            return new CommandParseResult(true, CommandUsage);
        
        }


       
    }
}