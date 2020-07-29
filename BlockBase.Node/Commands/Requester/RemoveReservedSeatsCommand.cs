using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Requester
{
    public class RemoveReservedSeatsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;

        private IList<string> _reservedSeatsToRemove;


        public override string CommandName => "Remove reserved seats";

        public override string CommandInfo => "Removes specified providers reserved seats";

        public override string CommandUsage => "rm reserved seats --accounts <accountName1> <accountName2> ...";

        public RemoveReservedSeatsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

          public RemoveReservedSeatsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IList<string> reservedSeatsToRemove) : this(logger, mainchainService, nodeConfigurations)
        {
            _reservedSeatsToRemove = reservedSeatsToRemove;
        }


        public override async Task<CommandExecutionResponse> Execute()
        {
            if (_reservedSeatsToRemove == null || _reservedSeatsToRemove.Count() == 0) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid account name"));
            try
            {
                var sidechainName = _nodeConfigurations.AccountName;
                var reservedSeatsTable = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                var sidechainStates = await _mainchainService.RetrieveContractState(sidechainName);
                var validAccounts = "";
                var seatsToRemove = new List<string>();

                if (sidechainStates == null || sidechainStates.IPReceiveTime || sidechainStates.IPSendTime || sidechainStates.SecretTime || !sidechainStates.Startchain)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"The {sidechainName} sidechain is not in the correct state or is not created."));

                foreach (var accountToRemove in _reservedSeatsToRemove)
                {
                    if (reservedSeatsTable.Any(p => p.Key == accountToRemove) && accountToRemove.Length >= 1)
                    {
                        seatsToRemove.Add(accountToRemove);
                        validAccounts += "[" + accountToRemove + "] ";
                    }
                }
                if (seatsToRemove.Count() == 0) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"None of the accounts inserted are eligible to get removed from the sidechain reserved seats."));
                var removeReservedSeatsTx = await _mainchainService.RemoveReservedSeats(sidechainName, seatsToRemove);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Accounts {validAccounts}successfully removed from the reserved seats of the sidechain. RemoveReservedSeats tx: {removeReservedSeatsTx}."));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length > 4;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("rm reserved seats");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            _reservedSeatsToRemove = new List<string>();
            if (commandData.Length > 4) 
            {
                for (var i = 4; i < commandData.Length; i++)
                {
                    _reservedSeatsToRemove.Add(commandData[i]);
                }
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}