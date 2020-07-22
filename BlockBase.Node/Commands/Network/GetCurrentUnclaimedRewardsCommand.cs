using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Endpoints;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Domain.Results;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Network
{
    public class GetCurrentUnclaimedRewardsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private string _accountName;

        private ILogger _logger;


        public override string CommandName => "Get current unclaimed rewards";

        public override string CommandInfo => "Retrieves current unclaimed rewards for specified account";

        public override string CommandUsage => "get rewards -acc <accountName>";

        public GetCurrentUnclaimedRewardsCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
             try
            {
                if (string.IsNullOrWhiteSpace(_accountName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid account name"));
                _accountName = _accountName.Trim();

                var rewardTable = await _mainchainService.RetrieveRewardTable(_accountName);
                if (rewardTable == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"The reward table for {_accountName} was not found"));



                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<(string provider, string reward)>>(rewardTable.Select(r => (r.Key, $"{Math.Round((double)r.Reward / 10000, 4)} BBT")).ToList()));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 4;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("get rewards");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 4)
            {
                if (commandData[2] != "--acc") return new CommandParseResult(true, CommandUsage);
                _accountName = commandData[3];
               
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }

    }
}