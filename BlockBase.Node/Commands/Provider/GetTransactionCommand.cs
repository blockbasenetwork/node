using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Provider
{
    public class GetTransactionCommand : AbstractCommand
    {

        private IMongoDbProducerService _mongoDbProducerService;

        private string _chainName;

        private ulong _transactionNumber;

        private ILogger _logger;


        public override string CommandName => "Get transaction";

        public override string CommandInfo => "Gets block with specified block number from specified sidechain";

        public override string CommandUsage => "get tx --chain <sidechainName> --n <transactionNumber>";

        public GetTransactionCommand(ILogger logger, IMongoDbProducerService mongoDbProducerService)
        {
            _logger = logger;
            _mongoDbProducerService = mongoDbProducerService;
        }

          public GetTransactionCommand(ILogger logger, IMongoDbProducerService mongoDbProducerService, string chainName, ulong transactionNumber) : this(logger, mongoDbProducerService)
        {
            _chainName = chainName;
            _transactionNumber = transactionNumber;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
              if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();

            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_chainName);

                if (!doesSidechainExist) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Sidechain not found"));

                var transaction = await _mongoDbProducerService.GetTransactionBySequenceNumber(_chainName, _transactionNumber);

                if (transaction == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Transaction not found"));

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<BlockBase.Domain.Blockchain.Transaction>(transaction));
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
            return commandStr.StartsWith("get tx");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 6) 
            {
                if(commandData[2] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[3];
                if(commandData[4] != "--n") return new CommandParseResult(true, CommandUsage);
                if( UInt64.TryParse(commandData[5], out var transactionNumber)) return new CommandParseResult(true, CommandUsage);
                _transactionNumber = transactionNumber;
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}