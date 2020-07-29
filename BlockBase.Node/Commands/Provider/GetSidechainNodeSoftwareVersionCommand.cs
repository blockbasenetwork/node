using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;

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

        public GetSidechainNodeSoftwareVersionCommand(ILogger logger, IMainchainService mainchainService, string chainName) : this(logger, mainchainService)
        {
            _chainName = chainName;
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