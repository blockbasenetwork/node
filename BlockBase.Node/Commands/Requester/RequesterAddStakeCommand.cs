using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Requester
{
    public class RequesterAddStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private double _stake;

        private ILogger _logger;

        
        public override string CommandName => "Add stake";

        public override string CommandInfo => "Adds stake to sidechain";

        public override string CommandUsage => "add req --stake <stakeValue>";

        public RequesterAddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public RequesterAddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, double stake) : this(logger, mainchainService, nodeConfigurations)
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
                var stakeString = $"{_stake.ToString("F4", CultureInfo.InvariantCulture)} BBT";
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
            return commandStr.StartsWith("add req");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 4)
            {
                if (commandData[2] != "--stake") return new CommandParseResult(true, CommandUsage);
                if (!Double.TryParse(commandData[3], out var stake)) return new CommandParseResult(true, "Unable to parse stake");
                _stake = stake;
                return new CommandParseResult(true, true);
            }
            
            return new CommandParseResult(true, CommandUsage);
        
        }

    }
}