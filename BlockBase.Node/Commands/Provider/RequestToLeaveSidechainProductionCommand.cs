using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using Microsoft.Extensions.Logging;


namespace BlockBase.Node.Commands.Provider
{
    public class RequestToLeaveSidechainProductionCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private IMongoDbProducerService _mongoDbProducerService;



        private ILogger _logger;

        private string _chainName;


        public override string CommandName => "Request to leave sidechain production";

        public override string CommandInfo => "Request to leave specified sidechain production";

        public override string CommandUsage => "req leave --chain <sidechainName>";

        public RequestToLeaveSidechainProductionCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IMongoDbProducerService mongoDbProducerService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _logger = logger;
        }

        public RequestToLeaveSidechainProductionCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IMongoDbProducerService mongoDbProducerService, string chainName) : this(logger, mainchainService, nodeConfigurations, mongoDbProducerService)
        {
            _chainName = chainName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();

            try
            {
                //TODO rpinto - to verify that a manual request to leave a sidechain shouldn't delete the database. That has to be done independently

                var chainContract = await _mainchainService.RetrieveContractState(_chainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(_chainName);
                var clientTable = await _mainchainService.RetrieveClientTable(_chainName);
                var producersTable = await _mainchainService.RetrieveProducersFromTable(_chainName);
                if (chainContract == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} not found"));
                if (candidatureTable == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Unable to retrieve {_chainName} candidature table"));

                var isProducerInCandidature = candidatureTable.Where(m => m.Key == _nodeConfigurations.AccountName).Any();
                var isProducerAnActiveProducer = producersTable.Where(m => m.Key == _nodeConfigurations.AccountName).Any();

                if (!isProducerInCandidature && !isProducerAnActiveProducer)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer {_nodeConfigurations.AccountName} not found in sidechain {_chainName}"));

                _logger.LogDebug($"Sending sidechain exit request for {_chainName}");
                var trx = await _mainchainService.SidechainExitRequest(_chainName);
                await _mongoDbProducerService.AddPastSidechainToDatabaseAsync(_chainName, clientTable.SidechainCreationTimestamp, false, LeaveNetworkReasonsConstants.EXIT_REQUEST);


                //TODO rpinto - needs to verify if exist request has been sent successfully



                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Exit successfully requested for {_chainName}. This node will automatically leave the sidehain after a full day has passed."));

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
            return commandStr.ToLower().StartsWith("req leave");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData[2] != "--chain") return new CommandParseResult(true, CommandUsage);
            _chainName = commandData[3];
            return new CommandParseResult(true, true);
        }

    }
}