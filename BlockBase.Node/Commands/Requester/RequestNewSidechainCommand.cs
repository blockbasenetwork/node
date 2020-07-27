using System;
using System.Collections.Generic;
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
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Requester
{
    public class RequestNewSidechainCommand : AbstractCommand
    {
        private IConnector _connector;

        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private RequesterConfigurations _requesterConfigurations;

        private ILogger _logger;


        public override string CommandName => "Request a new sidechain";

        public override string CommandInfo => "Sends a transaction to BlockBase Operations Contract to request a sidechain for configuration";

        public override string CommandUsage => "request sidechain [--stake <stakeValue>]";

        public RequestNewSidechainCommand(ILogger logger, IConnector connector, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, RequesterConfigurations requesterConfigurations)
        {
            _connector = connector;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _requesterConfigurations = requesterConfigurations;
            _logger = logger;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {

            if (await _connector.DoesDefaultDatabaseExist())
                return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "You already have databases associated to this requester node. Clear all of the node associated databases and keys with the command RemoveSidechainDatabasesAndKeys or create a new node with a new database prefix."));

            _connector.Setup().Wait();

            var contractSt = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            var networkInfo = await _mainchainService.GetInfo();
            var networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);
            if (contractSt != null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain {_nodeConfigurations.AccountName} already exists"));

            //Check configurations
            if (_requesterConfigurations.MaxBlockSizeInBytes <= BlockHeaderSizeConstants.BLOCKHEADER_MAX_SIZE)
                return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Configured block max size is lower than 205 bytes, please increase the size"));
            if (_requesterConfigurations.ValidatorNodes.RequiredNumber + _requesterConfigurations.HistoryNodes.RequiredNumber + _requesterConfigurations.FullNodes.RequiredNumber == 0)
                return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Requester configurations need to have at least one provider node requested for sidechain production"));
            if (_requesterConfigurations.BlockTimeInSeconds < 60 && networkName == EosNetworkNames.MAINNET)
                return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Block time needs to be 60 seconds or higher on Mainnet"));

            if (Stake > 0)
            {
                string stakeToInsert = Stake.ToString("F4") + " BBT";
                var stakeTransaction = await _mainchainService.AddStake(_nodeConfigurations.AccountName, _nodeConfigurations.AccountName, stakeToInsert);
                _logger.LogInformation("Stake sent to contract. Tx = " + stakeTransaction);
                _logger.LogInformation("Stake inserted = " + stakeToInsert);
            }

            //TODO rpinto - if ConfigureChain fails, will StartChain fail if run again, and thus ConfigureChain never be reached?
            var startChainTx = await _mainchainService.StartChain(_nodeConfigurations.AccountName, _nodeConfigurations.ActivePublicKey);
            var i = 0;


            var configuration = GetSidechainConfigurations();
            //TODO rpinto - review this while loop
            while (i < 3)
            {

                await Task.Delay(1000);

                try
                {
                    var minimumSoftwareVersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
                    var minimumSoftwareVersion = VersionHelper.ConvertFromVersionString(minimumSoftwareVersionString);
                    var configureTx = await _mainchainService.ConfigureChain(_nodeConfigurations.AccountName, configuration, _requesterConfigurations.ReservedProducerSeats, minimumSoftwareVersion);
                    return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Chain successfully created and configured. Start chain tx: {startChainTx}. Configure chain tx: {configureTx}"));
                }
                catch (ApiErrorException ex)
                {
                    _logger.LogInformation($"Failed {i + 1} times. Error: {ex.Message}");
                    i++;
                }
            }

            return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(new OperationCanceledException()));
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2 || commandData.Length == 4;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("request sidechain");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);
            if (commandData.Length == 4)
            {
                if (commandData[2] != "--stake") return new CommandParseResult(true, CommandUsage);
                if (!decimal.TryParse(commandData[3], out var stake)) return new CommandParseResult(true, "Unable to parse stake");
                Stake = stake;
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }


        private Dictionary<string, object> GetSidechainConfigurations()
        {
            var configurations = new ContractInformationTable();

            var numberOfProviders = _requesterConfigurations.FullNodes.RequiredNumber + _requesterConfigurations.HistoryNodes.RequiredNumber + _requesterConfigurations.ValidatorNodes.RequiredNumber;

            configurations.Key = _nodeConfigurations.AccountName;

            configurations.BlockTimeDuration = _requesterConfigurations.BlockTimeInSeconds;
            configurations.SizeOfBlockInBytes = _requesterConfigurations.MaxBlockSizeInBytes;
            configurations.NumberOfFullProducersRequired = _requesterConfigurations.FullNodes.RequiredNumber;
            configurations.NumberOfHistoryProducersRequired = _requesterConfigurations.HistoryNodes.RequiredNumber;
            configurations.NumberOfValidatorProducersRequired = _requesterConfigurations.ValidatorNodes.RequiredNumber;
            configurations.MaxPaymentPerBlockFullProducers = Convert.ToUInt64(10000 * _requesterConfigurations.FullNodes.MaxPaymentPerBlock);
            configurations.MaxPaymentPerBlockHistoryProducers = Convert.ToUInt64(10000 * _requesterConfigurations.HistoryNodes.MaxPaymentPerBlock);
            configurations.MaxPaymentPerBlockValidatorProducers = Convert.ToUInt64(10000 * _requesterConfigurations.ValidatorNodes.MaxPaymentPerBlock);
            configurations.MinPaymentPerBlockFullProducers = Convert.ToUInt64(10000 * _requesterConfigurations.FullNodes.MinPaymentPerBlock);
            configurations.MinPaymentPerBlockHistoryProducers = Convert.ToUInt64(10000 * _requesterConfigurations.HistoryNodes.MinPaymentPerBlock);
            configurations.MinPaymentPerBlockValidatorProducers = Convert.ToUInt64(10000 * _requesterConfigurations.ValidatorNodes.MinPaymentPerBlock);
            configurations.Stake = Convert.ToUInt64(10000 * _requesterConfigurations.MinimumProducerStake);

            configurations.CandidatureTime = _requesterConfigurations.BlockTimeInSeconds + ((numberOfProviders / 10) * 60);
            configurations.SendSecretTime = 60 + ((numberOfProviders / 10) * 60);
            configurations.SendTime = 60 + ((numberOfProviders / 10) * 60);
            configurations.ReceiveTime = 60 + ((numberOfProviders / 10) * 60);

            var mappedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configurations));

            return mappedConfig;
        }
    }
}