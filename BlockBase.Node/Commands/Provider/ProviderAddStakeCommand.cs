using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Provider
{
    public class ProviderAddStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private double _stake;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Add stake";

        public override string CommandInfo => "Adds stake to sidechain";

        public override string CommandUsage => "add prv --stake <stakeValue> --chain <sidechainName>";

        public ProviderAddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public ProviderAddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, double stake, string chainName) : this(logger, mainchainService, nodeConfigurations)
        {
            _stake = stake;
            _chainName = chainName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Please provide a valid sidechain name"));
            if (_stake <= 0) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Please provide a positive stake value"));
            _chainName = _chainName.Trim();

            try
            {
                var chainContract = await _mainchainService.RetrieveContractState(_chainName);
                if (chainContract == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} not found"));

                var stakeString = $"{_stake.ToString("F4", CultureInfo.InvariantCulture)} BBT";
                var trx = await _mainchainService.AddStake(_chainName, _nodeConfigurations.AccountName, stakeString);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Stake successfully added. Tx = {trx}"));
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
            return commandStr.StartsWith("add prv");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 6)
            {
                if (commandData[2] != "--stake") return new CommandParseResult(true, CommandUsage);
                if (!Double.TryParse(commandData[3], out var stake)) return new CommandParseResult(true, "Unable to parse stake");
                _stake = stake;
                if (commandData[4] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[5];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);

        }

    }
}