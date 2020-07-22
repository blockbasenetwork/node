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
    public class GetSidechainNodeSoftwareVersionCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Get sidechain node software version";

        public override string CommandInfo => "Get sidechain node software version";

        public override string CommandUsage => "version --chain <sidechainName>";

        public GetSidechainNodeSoftwareVersionCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();
            try
            {
                var versionInContract = await _mainchainService.RetrieveSidechainNodeVersion(_chainName);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Sidechain {_chainName} is running version {VersionHelper.ConvertFromVersionInt(versionInContract.SoftwareVersion)}"));
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
            return commandStr.StartsWith("version");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 3)
            {
                if (commandData[1] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[2];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}