using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;


namespace BlockBase.Node.Commands.Provider
{
    public class ProviderClaimStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Claim stake";

        public override string CommandInfo => "Claims stake from specified sidechain";

        public override string CommandUsage => "claim prv stake --chain <sidechainName>";

        public ProviderClaimStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public ProviderClaimStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, string chainName) : this(logger, mainchainService, nodeConfigurations)
        {
            _chainName = chainName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();
            
            try
            {
                var trx = await _mainchainService.ClaimStake(_chainName, _nodeConfigurations.AccountName);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Stake successfully claimed. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 5;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("claim prv stake");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 5)
            {
                if (commandData[3] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[4];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);

        }

    }
}