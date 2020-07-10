using System;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Node.Commands.Utils;

namespace BlockBase.Node.Commands.Requester
{
    public class RequestNewSidechainCommand : AbstractCommand
    {
        private IConnector _connector;
        
        public override string CommandName => "Request a new sidechain";

        public override string CommandInfo => "Sends a transaction to BlockBase Operations Contract to request a sidechain for configuration";

        public override string CommandUsage => "request sidechain [--stake <stakeValue>]";

        public RequestNewSidechainCommand(IConnector connector)
        {
            _connector = connector;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResult> Execute()
        {
            return null;
            // if (await _connector.DoesDefaultDatabaseExist())
            //         return BadRequest(new OperationResponse(false, "You already have databases associated to this requester node. Clear all of the node associated databases and keys with the command RemoveSidechainDatabasesAndKeys or create a new node with a new database prefix."));

            //     _connector.Setup().Wait();

            //     var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
            //     var networkInfo = await _mainchainService.GetInfo();
            //     var networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);
            //     if (contractSt != null) return BadRequest(new OperationResponse(false, $"Sidechain {NodeConfigurations.AccountName} already exists"));

            //     //Check configurations
            //     if (RequesterConfigurations.MaxBlockSizeInBytes <= BlockHeaderSizeConstants.BLOCKHEADER_MAX_SIZE)
            //         return BadRequest(new OperationResponse(false, $"Configured block max size is lower than 205 bytes, please increase the size"));
            //     if (RequesterConfigurations.ValidatorNodes.RequiredNumber + RequesterConfigurations.HistoryNodes.RequiredNumber + RequesterConfigurations.FullNodes.RequiredNumber == 0)
            //         return BadRequest(new OperationResponse(false, $"Requester configurations need to have at least one provider node requested for sidechain production"));
            //     if (RequesterConfigurations.BlockTimeInSeconds < 60 && networkName == EosNetworkNames.MAINNET)
            //         return BadRequest(new OperationResponse(false, $"Block time needs to be 60 seconds or higher on Mainnet"));

            //     if (stake > 0)
            //     {
            //         string stakeToInsert = stake.ToString("F4") + " BBT";
            //         var stakeTransaction = await _mainchainService.AddStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName, stakeToInsert);
            //         _logger.LogInformation("Stake sent to contract. Tx = " + stakeTransaction);
            //         _logger.LogInformation("Stake inserted = " + stakeToInsert);
            //     }

            //     //TODO rpinto - if ConfigureChain fails, will StartChain fail if run again, and thus ConfigureChain never be reached?
            //     var startChainTx = await _mainchainService.StartChain(NodeConfigurations.AccountName, NodeConfigurations.ActivePublicKey);
            //     var i = 0;


            //     var configuration = GetSidechainConfigurations();
            //     //TODO rpinto - review this while loop
            //     while (i < 3)
            //     {

            //         await Task.Delay(1000);

            //         try
            //         {
            //             var minimumSoftwareVersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
            //             var minimumSoftwareVersion = VersionHelper.ConvertFromVersionString(minimumSoftwareVersionString);
            //             var configureTx = await _mainchainService.ConfigureChain(NodeConfigurations.AccountName, configuration, RequesterConfigurations.ReservedProducerSeats, minimumSoftwareVersion);
            //             return Ok(new OperationResponse(true, $"Chain successfully created and configured. Start chain tx: {startChainTx}. Configure chain tx: {configureTx}"));
            //         }
            //         catch (ApiErrorException ex)
            //         {
            //             _logger.LogInformation($"Failed {i + 1} times. Error: {ex.Message}");
            //             i++;
            //         }
            //     }

            //     return StatusCode((int)HttpStatusCode.InternalServerError, new OperationCanceledException());
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
    }
}