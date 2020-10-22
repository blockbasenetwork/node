using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataProxy.Encryption;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;


namespace BlockBase.Node.Commands.Requester
{
    public class GenerateMasterKeyCommand : AbstractCommand
    {

        private ILogger _logger;


        public override string CommandName => "Generate Master Key";

        public override string CommandInfo => "Generates a master key to be used in encryption";

        public override string CommandUsage => "gen masterkey";

        public GenerateMasterKeyCommand(ILogger logger)
        {
            _logger = logger;
        }


        public override Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var key = KeyAndIVGenerator.CreateRandomKey();

                return Task.FromResult(new CommandExecutionResponse( HttpStatusCode.OK, new OperationResponse<string>(key, $"Master key successfully created. Master Key = {key}")));
            }
            catch (Exception e)
            {
                return Task.FromResult(new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e)));
            }

        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);
            return new CommandParseResult(true, CommandUsage);

        }

    }
}