using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;
namespace BlockBase.Node.Commands.Network
{
    public class GetAccountStakeRecordsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private string _accountName;

        private ILogger _logger;


        public override string CommandName => "Get account stake records";

        public override string CommandInfo => "Retrieves stake records of specified account";

        public override string CommandUsage => "get stake records --acc <accountName>";

        public GetAccountStakeRecordsCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

         public GetAccountStakeRecordsCommand(ILogger logger, IMainchainService mainchainService, string accountName) : this(logger, mainchainService)
        {
            _accountName = accountName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
           try
            {
                if (string.IsNullOrWhiteSpace(_accountName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid account name"));
                _accountName = _accountName.Trim();

                var stakeTable = await _mainchainService.RetrieveAccountStakedSidechains(_accountName);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<TokenLedgerTable>>(stakeTable));
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
            return commandStr.StartsWith("get stake records");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 5)
            {
                if (commandData[3] != "--acc") return new CommandParseResult(true, CommandUsage);
                _accountName = commandData[4];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}