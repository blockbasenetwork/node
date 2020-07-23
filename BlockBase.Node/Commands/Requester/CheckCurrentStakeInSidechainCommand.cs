using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Requester
{
    public class CheckCurrentStakeInSidechainCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;


        private ILogger _logger;

        
        public override string CommandName => "Check requester stake in sidechain";

        public override string CommandInfo => "Retrieves sidechain current stake";

        public override string CommandUsage => "check stake";

        public CheckCurrentStakeInSidechainCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations)
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
                var sidechainName = _nodeConfigurations.AccountName;
                var stakeLedger = await _mainchainService.RetrieveAccountStakedSidechains(sidechainName);
                var stakeRecord = stakeLedger.Where(o => o.Sidechain == sidechainName).FirstOrDefault();
                var stakeToReturn = stakeRecord != null ? stakeRecord.Stake : "0.0000 BBT";

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<string>(stakeToReturn, "Stake retrieved with success"));
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