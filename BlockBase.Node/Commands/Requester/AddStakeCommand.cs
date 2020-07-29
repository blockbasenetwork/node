using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Requester
{
    public class AddStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private double _stake;

        private ILogger _logger;

        
        public override string CommandName => "Add stake";

        public override string CommandInfo => "Adds stake to sidechain";

        public override string CommandUsage => "add --stake <stakeValue>";

        public AddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public AddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, double stake) : this(logger, mainchainService, nodeConfigurations)
        {
            _stake = stake;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (_stake <= 0)
            {
                return new CommandExecutionResponse( HttpStatusCode.BadRequest , new OperationResponse(false, "The stake must be positive"));
            }
            try
            {
                var stakeString = $"{_stake.ToString("F4")} BBT";
                var trx = await _mainchainService.AddStake(_nodeConfigurations.AccountName, _nodeConfigurations.AccountName, stakeString);

                return new CommandExecutionResponse( HttpStatusCode.OK, new OperationResponse(true, $"Stake successfully added. Tx = {trx}"));
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
            return commandStr.StartsWith("add");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 3)
            {
                if (commandData[1] != "--stake") return new CommandParseResult(true, CommandUsage);
                if (!Double.TryParse(commandData[2], out var stake)) return new CommandParseResult(true, "Unable to parse stake");
                _stake = stake;
                return new CommandParseResult(true, true);
            }
            
            return new CommandParseResult(true, CommandUsage);
        
        }

    }
}