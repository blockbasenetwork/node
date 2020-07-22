using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using BlockBase.Utils;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Provider
{
    public class AddStakeCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private double _stake;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Add stake";

        public override string CommandInfo => "Adds stake to sidechain";

        public override string CommandUsage => "add --stake <stakeValue> --chain <sidechainName>";

        public AddStakeCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
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

                var stakeString = $"{_stake.ToString("F4")} BBT";
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
            return commandStr.StartsWith("add");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 5)
            {
                if (commandData[1] != "--stake") return new CommandParseResult(true, CommandUsage);
                if (!Double.TryParse(commandData[2], out var stake)) return new CommandParseResult(true, "Unable to parse stake");
                _stake = stake;
                if (commandData[3] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[4];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);

        }

    }
}