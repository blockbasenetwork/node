using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Provider
{
    public class GetTransactionsInMempoolCommand : AbstractCommand
    {
        private IMongoDbProducerService _mongoDbProducerService;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Get transactions in mempool";

        public override string CommandInfo => "Gets transactions in mempool from specified sidechain";

        public override string CommandUsage => "get tx mempool --chain <sidechainName>";

        public GetTransactionsInMempoolCommand(ILogger logger, IMongoDbProducerService mongoDbProducerService)
        {
            _logger = logger;
            _mongoDbProducerService = mongoDbProducerService;
        }
         public GetTransactionsInMempoolCommand(ILogger logger, IMongoDbProducerService mongoDbProducerService, string chainName) : this(logger, mongoDbProducerService)
        {
            _chainName = chainName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();
            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_chainName);
                if (!doesSidechainExist) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Sidechain not found"));
                var transactionsInMempool = await _mongoDbProducerService.RetrieveTransactionsInMempool(_chainName);

                if (transactionsInMempool == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Sidechain not found."));

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<IEnumerable<BlockBase.Domain.Blockchain.Transaction>>(transactionsInMempool));
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
            return commandStr.StartsWith("get tx mempool");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 5)
            {
                if (commandData[3] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[4];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}