using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Provider
{
    public class GetProducingSidechainsCommand : AbstractCommand
    {
        private ISidechainProducerService _sidechainProducerService;

        private ILogger _logger;


        public override string CommandName => "Get producing sidechains";

        public override string CommandInfo => "Gets current producing sidechains";

        public override string CommandUsage => "get chains";

        public GetProducingSidechainsCommand(ILogger logger, ISidechainProducerService sidechainProducerService)
        {
            _sidechainProducerService = sidechainProducerService;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
           try
            {
                var poolOfSidechains = _sidechainProducerService.GetSidechainContexts();

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<string>>(poolOfSidechains.Select(s => s.SidechainPool.ClientAccountName).ToList(), $"Get producing sidechains successful."));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
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