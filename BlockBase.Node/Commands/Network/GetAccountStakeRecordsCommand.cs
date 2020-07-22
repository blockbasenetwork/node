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
using BlockBase.Domain.Eos;
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