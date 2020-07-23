using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;
namespace BlockBase.Node.Commands.Network
{
    public class GetSidechainStateCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Get sidechain state";

        public override string CommandInfo => "Retrieves specified sidechain state";

        public override string CommandUsage => "get st --chain <sidechainName>";

        public GetSidechainStateCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

          public GetSidechainStateCommand(ILogger logger, IMainchainService mainchainService, string chainName) : this(logger, mainchainService)
        {
            _chainName = chainName;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
          try
            {
                if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
                _chainName = _chainName.Trim();

                var contractState = await _mainchainService.RetrieveContractState(_chainName);
                var candidates = await _mainchainService.RetrieveCandidates(_chainName);
                var tokenLedger = await _mainchainService.GetAccountStake(_chainName, _chainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(_chainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(_chainName);
                var reservedSeats = await _mainchainService.RetrieveReservedSeatsTable(_chainName);

                if (contractState == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Contract state not found for {_chainName}"));
                if (candidates == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Candidate table not found for {_chainName}"));
                if (tokenLedger == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Token ledger table not found for {_chainName}"));
                if (producers == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer table not found for {_chainName}"));
                if (contractInfo == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Contract info not found for {_chainName}"));
                if (reservedSeats == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Reserved seats table not found for {_chainName}"));

                var slotsTakenByReservedSeats = 0;
                var fullNumberOfSlotsTakenByReservedSeats = 0;
                var historyNumberOfSlotsTakenByReservedSeats = 0;
                var validatorNumberOfSlotsTakenByReservedSeats = 0;

                foreach (var reservedSeatKey in reservedSeats.Select(r => r.Key).Distinct())
                {
                    var producer = producers.Where(o => o.Key == reservedSeatKey).SingleOrDefault();
                    if (producer != null)
                    {
                        slotsTakenByReservedSeats++;
                        if (producer.ProducerType == 3) fullNumberOfSlotsTakenByReservedSeats++;
                        if (producer.ProducerType == 2) historyNumberOfSlotsTakenByReservedSeats++;
                        if (producer.ProducerType == 1) validatorNumberOfSlotsTakenByReservedSeats++;
                    }
                }

                var sidechainState = new SidechainState()
                {

                    State = contractState.ConfigTime ? "Configure state" : contractState.SecretTime ? "Secrect state" : contractState.IPSendTime ? "Ip Send Time" : contractState.IPReceiveTime ? "Ip Receive Time" : contractState.ProductionTime ? "Production" : contractState.Startchain ? "Startchain" : "No State in chain",
                    StakeDepletionEndDate = StakeEndTimeCalculationAtMaxPayments(contractInfo, tokenLedger),
                    CurrentRequesterStake = tokenLedger.StakeString,
                    InProduction = contractState.ProductionTime,
                    ReservedSeats = new ReservedSeats()
                    {
                        TotalNumber = reservedSeats.Count,
                        SlotsStillAvailable = reservedSeats.Count - slotsTakenByReservedSeats,
                        SlotsTaken = slotsTakenByReservedSeats
                    },
                    FullProducersInfo = new SidechainProducersInfo()
                    {
                        NumberOfProducersRequired = (int)contractInfo.NumberOfFullProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 3).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 3).Count(),
                        NumberOfSlotsTakenByReservedSeats = fullNumberOfSlotsTakenByReservedSeats

                    },
                    HistoryProducersInfo = new SidechainProducersInfo()
                    {
                        NumberOfProducersRequired = (int)contractInfo.NumberOfHistoryProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 2).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 2).Count(),
                        NumberOfSlotsTakenByReservedSeats = historyNumberOfSlotsTakenByReservedSeats
                    },
                    ValidatorProducersInfo = new SidechainProducersInfo()
                    {
                        NumberOfProducersRequired = (int)contractInfo.NumberOfValidatorProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 1).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 1).Count(),
                        NumberOfSlotsTakenByReservedSeats = validatorNumberOfSlotsTakenByReservedSeats
                    }
                };
                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<SidechainState>(sidechainState));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 4;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("get st");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 4)
            {
                if (commandData[2] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[3];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }

        private DateTime StakeEndTimeCalculationAtMaxPayments(ContractInformationTable contractInfo, AccountStake sidechainStake)
        {
            var blocksDividedByTotalNumberOfProducers = contractInfo.BlocksBetweenSettlement / (contractInfo.NumberOfFullProducersRequired + contractInfo.NumberOfHistoryProducersRequired + contractInfo.NumberOfValidatorProducersRequired);
            var fullProducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfFullProducersRequired) * contractInfo.MaxPaymentPerBlockFullProducers;
            var historyroducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfHistoryProducersRequired) * contractInfo.MaxPaymentPerBlockHistoryProducers;
            var validatorProducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfValidatorProducersRequired) * contractInfo.MaxPaymentPerBlockFullProducers;

            var sidechainStakeString = sidechainStake.StakeString.Split(" ")[0];
            var sidechainStakeInUnitsString = sidechainStakeString.Split(".")[0] + sidechainStakeString.Split(".")[1];

            var timesThatRequesterCanPaySettlementWithAllProvidersAtMaxPrice = ulong.Parse(sidechainStakeInUnitsString) / ((fullProducerPaymentPerSettlement + historyroducerPaymentPerSettlement + validatorProducerPaymentPerSettlement));
            return DateTime.UtcNow.AddSeconds((contractInfo.BlockTimeDuration * contractInfo.BlocksBetweenSettlement) * timesThatRequesterCanPaySettlementWithAllProvidersAtMaxPrice);
        }
    }
}