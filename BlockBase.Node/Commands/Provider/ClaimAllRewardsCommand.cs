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
    public class ClaimAllRewardsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;


        public override string CommandName => "Claim all rewards";

        public override string CommandInfo => "Claims stake from all sidechains";

        public override string CommandUsage => "claim all";

        public ClaimAllRewardsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
               try
            {
                var accountName = _nodeConfigurations.AccountName;
                var rewardTable = await _mainchainService.RetrieveRewardTable(accountName);
                if (rewardTable == null) return new CommandExecutionResponse( HttpStatusCode.NotFound, new OperationResponse(false, $"The reward table for {accountName} was not found"));


                long totalClaimed = 0;
                foreach (var rewardToClaim in rewardTable)
                {
                    if (rewardToClaim.Reward > 0)
                    {
                        try
                        {
                            await _mainchainService.ClaimReward(rewardToClaim.Key, accountName);
                            _logger.LogInformation($"Claimed {Math.Round((double)rewardToClaim.Reward / 10000, 4)} BBT from {rewardToClaim.Key}");
                            totalClaimed += rewardToClaim.Reward;
                        }
                        catch { }
                    }
                }


                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Successfully claimed {Math.Round((double)totalClaimed / 10000, 4)} BBT"));
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
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);

            return new CommandParseResult(true, CommandUsage);
        }
    }
}