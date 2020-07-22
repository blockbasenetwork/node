using System;
using System.Collections.Generic;
using System.Linq;
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
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Requester
{
    public class CheckSidechainReservedSeatsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;


        public override string CommandName => "Check sidechain reserved seats";

        public override string CommandInfo => "Retrieves providers reserved seats";

        public override string CommandUsage => "check reserved seats";

        public CheckSidechainReservedSeatsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }


        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var sidechainName = _nodeConfigurations.AccountName;
                var reservedSeatsTable = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                var sidechainStates = await _mainchainService.RetrieveContractState(sidechainName);

                if (sidechainStates == null)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"The {sidechainName} sidechain is not created."));


                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<ReservedSeatsTable>>(reservedSeatsTable));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 3;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 3) return new CommandParseResult(true, true);

            return new CommandParseResult(true, CommandUsage);
        }
    }
}