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
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Network
{
    public class GetSidechainConfigurationCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Get sidechain configuration";

        public override string CommandInfo => "Retrieves specified sidechain configuration";

        public override string CommandUsage => "get config --chain <sidechainName>";

        public GetSidechainConfigurationCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
          try
            {
                if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a sidechain name."));
                _chainName = _chainName.Trim();

                ContractInformationTable contractInfo = await _mainchainService.RetrieveContractInformation(_chainName);

                if (contractInfo == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} configuration not found"));

                var result = new GetSidechainConfigurationModel
                {
                    account_name = contractInfo.Key,
                    BlocksBetweenSettlement = contractInfo.BlocksBetweenSettlement,
                    BlockTimeDuration = contractInfo.BlockTimeDuration,
                    CandidatureEndDate = DateTimeOffset.FromUnixTimeSeconds(contractInfo.CandidatureEndDate).DateTime,
                    CandidatureTime = contractInfo.CandidatureTime,
                    MaxPaymentPerBlockFullProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockFullProducers / 10000, 4),
                    MaxPaymentPerBlockHistoryProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockHistoryProducers / 10000, 4),
                    MaxPaymentPerBlockValidatorProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockValidatorProducers / 10000, 4),
                    MinPaymentPerBlockFullProducers = Math.Round((decimal)contractInfo.MinPaymentPerBlockFullProducers / 10000, 4),
                    MinPaymentPerBlockHistoryProducers = Math.Round((decimal)contractInfo.MinPaymentPerBlockHistoryProducers / 10000, 4),
                    MinPaymentPerBlockValidatorProducers = Math.Round((decimal)contractInfo.MinPaymentPerBlockValidatorProducers / 10000, 4),
                    Stake = Math.Round((decimal)contractInfo.Stake / 10000, 4),
                    NumberOfFullProducersRequired = contractInfo.NumberOfFullProducersRequired,
                    NumberOfHistoryProducersRequired = contractInfo.NumberOfHistoryProducersRequired,
                    NumberOfValidatorProducersRequired = contractInfo.NumberOfValidatorProducersRequired,
                    ReceiveEndDate = contractInfo.ReceiveEndDate,
                    ReceiveTime = contractInfo.ReceiveTime,
                    SecretEndDate = contractInfo.SecretEndDate,
                    SendEndDate = contractInfo.SendEndDate,
                    SendSecretTime = contractInfo.SendSecretTime,
                    SendTime = contractInfo.SendTime,
                    SizeOfBlockInBytes = contractInfo.SizeOfBlockInBytes
                };

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<dynamic>(result));
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
            return commandStr.StartsWith("get config");
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
    }
}