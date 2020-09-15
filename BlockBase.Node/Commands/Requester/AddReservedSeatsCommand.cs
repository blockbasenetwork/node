using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Requester
{
    public class AddReservedSeatsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;

        private IList<ReservedSeatConfig> _reservedSeatsToAdd;


        public override string CommandName => "Add reserved seats";

        public override string CommandInfo => "Add specified providers reserved seats";

        public override string CommandUsage => "add reserved seats --accounts <accountName1> <accountName2> ...";

        public AddReservedSeatsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public AddReservedSeatsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IList<ReservedSeatConfig> reservedSeatsToAdd) : this(logger, mainchainService, nodeConfigurations)
        {

            _reservedSeatsToAdd = reservedSeatsToAdd;
        }


        public override async Task<CommandExecutionResponse> Execute()
        {
            if (_reservedSeatsToAdd == null || _reservedSeatsToAdd.Count() == 0) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid account name"));
            try
            {
                var sidechainName = _nodeConfigurations.AccountName;

                var reservedSeatsTable = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                var sidechainStates = await _mainchainService.RetrieveContractState(sidechainName);
                var responseString = "";
                var listToAdd = new List<Dictionary<string, object>>();

                if (sidechainStates == null || sidechainStates.IPReceiveTime || sidechainStates.IPSendTime || sidechainStates.SecretTime || !sidechainStates.Startchain)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"The {sidechainName} sidechain is not in the correct state or is not created."));

                foreach (var accountToAdd in _reservedSeatsToAdd)
                {
                    if (!reservedSeatsTable.Any(p => p.Key == accountToAdd.Account) && accountToAdd.Account != null && accountToAdd.Account.Length > 0)
                    {
                        responseString += " [" + accountToAdd + "] ";
                        var reservedSeat = new ReservedSeatsTable()
                        {
                            Key = accountToAdd.Account,
                            ProducerType = (uint)accountToAdd.ProducerType
                        };
                        listToAdd.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(reservedSeat)));
                    }
                }

                if (listToAdd.Count() == 0) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"None of the accounts inserted are eligible to get added to the sidechain reserved seats."));
                var addReserverSeatTx = await _mainchainService.AddReservedSeats(sidechainName, listToAdd);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Reserved seats successfully added for the accounts{responseString} if they exist in EOSIO network. AddReservedSeats tx: {addReserverSeatTx}"));
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
            return commandStr.StartsWith("add reserved seats");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            _reservedSeatsToAdd = new List<ReservedSeatConfig>();
            if (commandData.Length > 4)
            {
                for (var i = 4; i < commandData.Length; i++)
                {
                    _reservedSeatsToAdd.Add(new ReservedSeatConfig(){
                        Account = commandData[i], 
                        ProducerType = Convert.ToInt32(commandData[i+1])
                    });
                }
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}