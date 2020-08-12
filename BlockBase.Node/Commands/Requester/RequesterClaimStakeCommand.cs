using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Requester
{
    public class RequesterClaimStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;

        
        public override string CommandName => "Claim stake";

        public override string CommandInfo => "Claims sidechain leftover stake";

        public override string CommandUsage => "claim req stake";

        public RequesterClaimStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
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
            return commandData.Length == 3;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length != 3) return new CommandParseResult(false, false);
            return new CommandParseResult(true, true);
        
        }


       
    }
}