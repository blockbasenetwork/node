using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Provider
{
    public class RemoveCandidatureCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private IMongoDbProducerService _mongoDbProducerService;

        private ISidechainProducerService _sidechainProducerService;



        private ILogger _logger;

        private string _chainName;


        public override string CommandName => "Remove candidature";

        public override string CommandInfo => "Removes candidature of specified sidechain";

        public override string CommandUsage => "rm cand --chain <sidechainName>";

        public RemoveCandidatureCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, ISidechainProducerService sidechainProducerService, IMongoDbProducerService mongoDbProducerService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainProducerService = sidechainProducerService;
            _logger = logger;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();
            try
            {

                var chainContract = await _mainchainService.RetrieveContractState(_chainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(_chainName);
                if (chainContract == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} not found"));
                if (candidatureTable == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Unable to retrieve {_chainName} candidature table"));

                var isProducerInCandidature = candidatureTable.Where(m => m.Key == _nodeConfigurations.AccountName).Any();

                if (!isProducerInCandidature)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer {_nodeConfigurations.AccountName} not found in sidechain {_chainName}"));

                if (!chainContract.CandidatureTime)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain is not in candidature time so candidature can't be removed"));

                var trx = await _mainchainService.RemoveCandidature(_chainName, _nodeConfigurations.AccountName);
                _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(_chainName);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Candidature succesfully removed from {_chainName}. Tx: {trx}"));

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
            return commandStr.ToLower().StartsWith("rm cand");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData[2] != "--chain") return new CommandParseResult(true, CommandUsage);
            _chainName = commandData[3];
            return new CommandParseResult(true, true);
        }

    }
}