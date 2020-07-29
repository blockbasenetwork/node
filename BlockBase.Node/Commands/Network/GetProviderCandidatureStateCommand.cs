using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Network
{
    public class GetProviderCandidatureStateCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private string _chainName;

        private string _accountName;

        private ILogger _logger;


        public override string CommandName => "Get provider candidature state";

        public override string CommandInfo => "Retrieves provider candidature state in specified sidechain";

        public override string CommandUsage => "get candst --acc <accountName> --chain <sidechainName>";

        public GetProviderCandidatureStateCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

        public GetProviderCandidatureStateCommand(ILogger logger, IMainchainService mainchainService, string chainName, string accountName) : this(logger, mainchainService)
        {
            _chainName = chainName;
            _accountName = accountName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_accountName) || string.IsNullOrWhiteSpace(_chainName))
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Please provide and producer account name and a sidechain name"));
                }
                _accountName = _accountName.Trim();
                _chainName = _chainName.Trim();

                var contractState = await _mainchainService.RetrieveContractState(_chainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(_chainName);
                var producerTable = await _mainchainService.RetrieveProducersFromTable(_chainName);

                if (contractState == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Unable to retrieve {_chainName} contract state"));
                if (candidatureTable == null && producerTable == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Unable to retrieve {_chainName} candidature and production table"));

                if (candidatureTable != null && candidatureTable.Where(m => m.Key == _accountName).Any())
                    return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(false, $"Account {_accountName} has applied for {_chainName}"));

                if (producerTable != null && producerTable.Where(m => m.Key == _accountName).Any())
                    return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(false, $"Account {_accountName} is producing for {_chainName}"));

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(false, $"Producer {_accountName} not found"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 6;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("get candst");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 6)
            {
                if (commandData[2] != "--acc") return new CommandParseResult(true, CommandUsage);
                _accountName = commandData[3];
                if (commandData[4] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[5];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}