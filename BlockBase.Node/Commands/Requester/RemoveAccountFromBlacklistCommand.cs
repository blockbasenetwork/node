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
    public class RemoveAccountFromBlacklistCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;

        private string _account;


        public override string CommandName => "Remove account from blacklist";

        public override string CommandInfo => "Removes specified account from blacklist";

        public override string CommandUsage => "rm bl --account <accountName>";

        public RemoveAccountFromBlacklistCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

         public RemoveAccountFromBlacklistCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, string accountName) : this(logger, mainchainService, nodeConfigurations)
        {
            _account = accountName;
        }


        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_account)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid account name"));
            _account = _account.Trim();
            try
            {
                var sidechainName = _nodeConfigurations.AccountName;

                var blacklist = await _mainchainService.RetrieveBlacklistTable(sidechainName);

                if (!blacklist.Any(p => p.Key == _account))
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer {_account} isn't in the blacklist for sidechain {sidechainName}"));

                var trx = await _mainchainService.RemoveBlacklistedProducer(sidechainName, _account);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Account {_account} successfully removed from blacklist"));
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
            return commandStr.StartsWith("rm bl");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length != 4) return new CommandParseResult(true, CommandUsage);
            _account = commandData[3];

            return new CommandParseResult(true, true);

        }



    }
}