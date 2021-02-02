using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Provider
{
    public class RequestToProduceSidechainCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private IMongoDbProducerService _mongoDbProducerService;

        private ISidechainProducerService _sidechainProducerService;


        
        private ILogger _logger;

        private string _chainName;

        private int _providerType;

        private decimal _stake;
 


        public override string CommandName => "Request to produce sidechain";

        public override string CommandInfo => "Requests to produce in specified sidechain with a defined stake as a certain producer type. Provider type: 1-Validator Node, 2-History Node and 3-Full Node.";

        public override string CommandUsage => "req prod --chain <sidechainName> --providerType <intProducerType> --stake <stake>";

        public RequestToProduceSidechainCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, ISidechainProducerService sidechainProducerService, IMongoDbProducerService mongoDbProducerService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainProducerService = sidechainProducerService;
            _logger = logger;
        }

          public RequestToProduceSidechainCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, ISidechainProducerService sidechainProducerService, IMongoDbProducerService mongoDbProducerService, string chainName, int providerType, decimal stake) : this(logger, mainchainService, nodeConfigurations, sidechainProducerService, mongoDbProducerService)
        {
            _chainName = chainName;
            this._providerType = providerType;
            _stake = stake;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
             //TODO rpinto - to verify when done. The request to produce a sidechain won't be allowed if there still exists data related to that sidechain on the database
            //The user will have to delete it manually. This only happens if the user registered on the sidechain manually too

            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            if (_providerType < 1 || _providerType > 3) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid provider type. (1) Validator, (2) History, (3) Full"));
            if (_stake < 0) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a non-negative stake value"));
            _chainName = _chainName.Trim();

            try
            {
                var chainContract = await _mainchainService.RetrieveContractState(_chainName);
                if (chainContract == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} not found"));

                var clientInfo = await _mainchainService.RetrieveClientTable(_chainName);
                if (clientInfo == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} client info not found"));

                var contractInfo = await _mainchainService.RetrieveContractInformation(_chainName);
                if (contractInfo == null) return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Sidechain {_chainName} contract info not found"));

                var candidates = await _mainchainService.RetrieveCandidates(_chainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(_chainName);
                var reservedSeats = await _mainchainService.RetrieveReservedSeatsTable(_chainName);
                var isProducerInTable = producers.Any(c => c.Key == _nodeConfigurations.AccountName);
                var isCandidateInTable = candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

                if (!chainContract.CandidatureTime && !isProducerInTable && !isCandidateInTable) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain not in candidature time and provider not in producer or candidate tables"));

                var networkInfo = await _mainchainService.GetInfo();
                var networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);

                var softwareVersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
                var softwareVersion = VersionHelper.ConvertFromVersionString(softwareVersionString);
                var versionInContract = await _mainchainService.RetrieveSidechainNodeVersion(_chainName);
                if (versionInContract.SoftwareVersion > softwareVersion)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain is running version {VersionHelper.ConvertFromVersionInt(versionInContract.SoftwareVersion)} while current node is running version {softwareVersionString}"));

                //if the chain exists in the pool it should mean that he's associated with it
                var chainExistsInPool = _sidechainProducerService.DoesChainExist(_chainName);
                if (chainExistsInPool)
                {
                    var sidechainContext = _sidechainProducerService.GetSidechainContext(_chainName);

                    //if it's running he should need to do anything because the state manager will decide what to do
                    if (sidechainContext.SidechainStateManager.TaskContainer.IsRunning())
                        return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Request to produce sidechain {_chainName} previously sent."));
                    //if it's not running, there was a problem and it should be removed from the pool list
                    else
                    {
                        //if chain exists in pool and isn't running, remove it
                        //this also means that there should be remnants of the database
                        _logger.LogDebug($"Removing sidechain {_chainName} execution engine");
                        _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(_chainName);
                    }
                }

                if(reservedSeats.SingleOrDefault(r => r.Key == _nodeConfigurations.AccountName) == null &&
                   ((contractInfo.NumberOfFullProducersRequired == 0 && _providerType == 3) || 
                   (contractInfo.NumberOfHistoryProducersRequired == 0 && _providerType == 2) || 
                   (contractInfo.NumberOfValidatorProducersRequired == 0 && _providerType == 1)))
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer type inserted is not needed in the given sidechain configuration"));
                if (contractInfo.BlockTimeDuration < 60 && networkName == EosNetworkNames.MAINNET)
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain has block time lower than 60 seconds running on mainnet and can not be joined"));
                
                var isPublicKeyAlreadyUsed = producers.Any(p => p.PublicKey == _nodeConfigurations.ActivePublicKey && p.Key != _nodeConfigurations.AccountName) || candidates.Any(c => c.PublicKey == _nodeConfigurations.ActivePublicKey && c.Key != _nodeConfigurations.AccountName);
                if (isPublicKeyAlreadyUsed) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Key {_nodeConfigurations.ActivePublicKey} is already being used by another producer or candidate"));

                var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_chainName);
                var firstBlockInBlockHeaderList = (await _mainchainService.RetrieveBlockheaderList(_chainName, (int)contractInfo.BlocksBetweenSettlement)).FirstOrDefault();
                //if the database exists and he's on the producer table, then nothing should be done
                if (chainExistsInDb && isProducerInTable)
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"{_nodeConfigurations.AccountName} is a provider in {_chainName}"));
                }
                //if he's not a producer, but is requesting again to be one, and has a database associated, he should delete it first
                else if (chainExistsInDb && (firstBlockInBlockHeaderList == null || !(await _mongoDbProducerService.IsBlockInDatabase(_chainName, firstBlockInBlockHeaderList.BlockHash))))
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"There is a database related to this chain. Please delete it"));
                }

                var accountStake = await _mainchainService.GetAccountStake(_chainName, _nodeConfigurations.AccountName);
                var minimumProviderState = Math.Round((decimal)contractInfo.Stake / 10000, 4);
                if (minimumProviderState > accountStake?.Stake + _stake)
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Minimum provider stake is {minimumProviderState}, currently staked {accountStake?.Stake} and added {_stake} which is not enough. Please stake {minimumProviderState - accountStake?.Stake}"));
                }
                
                if (!chainExistsInDb)
                {
                    await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(_chainName, clientInfo.SidechainCreationTimestamp, false, _providerType);
                }
                
                if (_stake > 0)
                {
                    var stakeTransaction = await _mainchainService.AddStake(_chainName, _nodeConfigurations.AccountName, _stake.ToString("F4", CultureInfo.InvariantCulture) + " BBT");
                    _logger.LogInformation("Sent stake to contract. Tx = " + stakeTransaction);
                    _logger.LogInformation("Stake inserted = " + _stake.ToString("F4", CultureInfo.InvariantCulture) + " BBT");
                }

                await _sidechainProducerService.AddSidechainToProducerAndStartIt(_chainName, clientInfo.SidechainCreationTimestamp, _providerType, false);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, "Candidature successfully added"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 8;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.ToLower().StartsWith("req prod");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if(commandData[2] != "--chain") return new CommandParseResult(true, CommandUsage);
            _chainName = commandData[3];
            if(commandData[4] != "--providerType") return new CommandParseResult(true, CommandUsage);
            if(!Int32.TryParse(commandData[5], out int providerType)) return new CommandParseResult(true, CommandUsage);
            this._providerType = providerType;
            if(commandData[6] != "--stake") return new CommandParseResult(true, CommandUsage);
            if(!Decimal.TryParse(commandData[7], out decimal stake)) return new CommandParseResult(true, CommandUsage);
            _stake = stake;
            return new CommandParseResult(true, true);
        }

    }
}