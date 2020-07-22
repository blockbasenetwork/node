using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
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
    public class GetBlockCommand : AbstractCommand
    {
        private ISidechainProducerService _sidechainProducerService;

        private IMongoDbProducerService _mongoDbProducerService;

        private string _chainName;

        private ulong _blockNumber;

        private ILogger _logger;


        public override string CommandName => "Get block";

        public override string CommandInfo => "Gets block with specified block number from specified sidechain";

        public override string CommandUsage => "get block --chain <sidechainName> --n <blockNumber>";

        public GetBlockCommand(ILogger logger, ISidechainProducerService sidechainProducerService,  IMongoDbProducerService mongoDbProducerService)
        {
            _sidechainProducerService = sidechainProducerService;
            _logger = logger;
            _mongoDbProducerService = mongoDbProducerService;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
             if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();

            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_chainName);

                if (!doesSidechainExist) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Sidechain not found"));

                var blockResponse = await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(_chainName, _blockNumber, _blockNumber);
                var block = blockResponse.SingleOrDefault();

                if (block == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Block not found"));

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<Block>(block));
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
            return commandStr.StartsWith("get block");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 6) 
            {
                if(commandData[2] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[3];
                if(commandData[4] != "--n") return new CommandParseResult(true, CommandUsage);
                if( UInt64.TryParse(commandData[5], out var blockNumber)) return new CommandParseResult(true, CommandUsage);
                _blockNumber = blockNumber;
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}