using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using System.Text;
using System.Linq;
using BlockBase.Utils.Crypto;
using BlockBase.DataPersistence.Data;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Domain.Enums;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Blockchain;
using BlockBase.Runtime.Provider;
using Newtonsoft.Json;
using System.Globalization;
using BlockBase.Node.Filters;
using BlockBase.Utils;
using System.Reflection;
using BlockBase.Domain.Eos;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "providerApi")]
    [ServiceFilter(typeof(ApiKeyAttribute))]
    public class ProviderController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private NetworkConfigurations NetworkConfigurations;
        private ApiSecurityConfigurations ApiSecurityConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private IConnectionsChecker _connectionsChecker;

        public ProviderController(ILogger<ProviderController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, IOptions<ApiSecurityConfigurations> apiSecurityConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, IConnectionsChecker connectionsChecker)
        {
            NodeConfigurations = nodeConfigurations?.Value;
            NetworkConfigurations = networkConfigurations?.Value;
            ApiSecurityConfigurations = apiSecurityConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _connectionsChecker = connectionsChecker;

        }

        /// <summary>
        /// Checks if the BlockBase node is correctly configured to work as a provider
        /// </summary>
        /// <returns>Information about the node configurations and account state</returns>
        /// <response code="200">Information about the current configuration</response>
        /// <response code="500">Some internal error occurred</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Checks if the BlockBase node is correctly configured",
            Description = "Before starting a node as a provider, the admin should check if everything is correctly configured",
            OperationId = "CheckProducerConfig"
        )]
        public async Task<ObjectResult> CheckProducerConfig()
        {
            try
            {

                bool fetchedExternalUtcTimeReference = false;
                TimeSpan timeDifference = TimeSpan.FromSeconds(0);

                DateTime machineUtcDateTime = DateTime.UtcNow;
                DateTime externalUtcDateTime = DateTime.MinValue;



                try
                {
                    var webClient = new WebClient();
                    var result = webClient.DownloadString(new Uri("http://worldtimeapi.org/api/timezone/Etc/UTC"));
                    machineUtcDateTime = DateTime.UtcNow;

                    if (string.IsNullOrWhiteSpace(result))
                        fetchedExternalUtcTimeReference = false;

                    var obj = new { datetime = string.Empty };

                    var jsonResult = JsonConvert.DeserializeObject(result, obj.GetType());

                    string dateTimeToParse = ((dynamic)jsonResult).datetime;
                    DateTime parsedTime;
                    if (!DateTime.TryParse(dateTimeToParse, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsedTime))
                        fetchedExternalUtcTimeReference = false;

                    fetchedExternalUtcTimeReference = true;
                    externalUtcDateTime = parsedTime;
                    timeDifference = machineUtcDateTime - externalUtcDateTime;
                }
                catch
                {

                }


                string configuredPublicIp = NetworkConfigurations.PublicIpAddress.Trim();
                string fetchedPublicIp = null;
                bool fetchedPublicIpSuccessfully = false;
                bool isConfiguredIPEqualToPublicIP = false;


                try
                {
                    var webClient = new WebClient();
                    var result = webClient.DownloadString(new Uri("https://api.ipify.org"));
                    fetchedPublicIpSuccessfully = !string.IsNullOrWhiteSpace(result.Trim());
                    fetchedPublicIp = result.Trim();
                    isConfiguredIPEqualToPublicIP = configuredPublicIp == fetchedPublicIp;
                }
                catch
                {

                }

                var isMongoLive = await _connectionsChecker.IsAbleToConnectToMongoDb();
                var isPostgresLive = await _connectionsChecker.IsAbleToConnectToPostgres();

                var accountName = NodeConfigurations.AccountName;
                var activePublicKey = NodeConfigurations.ActivePublicKey;


                bool eosAccountDataFetched = false;
                List<string> currencyBalance = null;
                long cpuUsed = 0;
                long cpuLimit = 0;
                long netUsed = 0;
                long netLimit = 0;
                ulong ramUsed = 0;
                long ramLimit = 0;

                bool activeKeyFoundOnAccount = false;
                bool activeKeyHasEnoughWeight = false;


                try
                {
                    var accountInfo = await _mainchainService.GetAccount(NodeConfigurations.AccountName);
                    currencyBalance = await _mainchainService.GetCurrencyBalance(NetworkConfigurations.BlockBaseTokenContract, NodeConfigurations.AccountName);

                    eosAccountDataFetched = true;
                    cpuUsed = accountInfo.cpu_limit.used;
                    cpuLimit = accountInfo.cpu_limit.max;
                    netUsed = accountInfo.net_limit.used;
                    netLimit = accountInfo.net_limit.max;
                    ramUsed = accountInfo.ram_usage;
                    ramLimit = accountInfo.ram_quota;

                    var permission = accountInfo.permissions.SingleOrDefault(p => p.perm_name == "active");

                    if (permission != null)
                    {
                        var correspondingActiveKey = permission.required_auth?.keys?.SingleOrDefault(k => k.key == activePublicKey);
                        if (correspondingActiveKey != null)
                            activeKeyFoundOnAccount = true;
                        if (correspondingActiveKey != null && correspondingActiveKey.weight >= permission.required_auth.threshold)
                            activeKeyHasEnoughWeight = true;

                    }


                }
                catch { }


                var tcpPort = NetworkConfigurations.TcpPort;

                var mongoDbConnectionString = NodeConfigurations.MongoDbConnectionString;
                var mongoDbPrefix = NodeConfigurations.DatabasesPrefix;

                var postgresHost = NodeConfigurations.PostgresHost;
                var postgresPort = NodeConfigurations.PostgresPort;
                var postgresUser = NodeConfigurations.PostgresUser;

                return Ok(new OperationResponse<dynamic>(
                    new
                    {
                        fetchedExternalUtcTimeReference,
                        machineUtcDateTime,
                        externalUtcDateTime,
                        timeDifference,

                        configuredPublicIp,
                        fetchedPublicIpSuccessfully,
                        fetchedPublicIp,
                        isConfiguredIPEqualToPublicIP,

                        tcpPort,
                        accountName,
                        eosAccountDataFetched,
                        activePublicKey,
                        activeKeyFoundOnAccount,
                        activeKeyHasEnoughWeight,
                        currencyBalance,
                        cpuUsed,
                        cpuLimit,
                        netUsed,
                        netLimit,
                        ramUsed,
                        ramLimit,

                        mongoDbConnectionString,
                        mongoDbPrefix,
                        isMongoLive,
                        postgresHost,
                        postgresPort,
                        postgresUser,
                        isPostgresLive,
                    }
                    , $"Configuration and connection data retrieved."));

            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract that contains the provider application information for producing the sidechain
        /// </summary>
        /// <param name="chainName">Account name of the sidechain</param>
        /// <param name="stake">The amount of BBT that the provider want's to stake</param>
        /// <param name="providerType">The type of provider the node is going to be for this sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Candidature sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="404">Sidechain not found</response>
        /// <response code="500">Error sending candidature</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract that contains the provider application information for producing the sidechain",
            Description = "The provider uses this service to apply to producing a specific sidechain. With this service, they send information about how much time in seconds they are willing to work on that sidechain",
            OperationId = "RequestToProduceSidechain"
        )]
        public async Task<ObjectResult> RequestToProduceSidechain(string chainName, int providerType, decimal stake = 0)
        {
            //TODO rpinto - to verify when done. The request to produce a sidechain won't be allowed if there still exists data related to that sidechain on the database
            //The user will have to delete it manually. This only happens if the user registered on the sidechain manually too

            if (string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            if (providerType < 1 || providerType > 3) return BadRequest(new OperationResponse(false, "Please provide a valid provider type. (1) Validator, (2) History, (3) Full"));
            if (stake < 0) return BadRequest(new OperationResponse(false, "Please provide a non-negative stake value"));
            chainName = chainName.Trim();

            try
            {
                var chainContract = await _mainchainService.RetrieveContractState(chainName);
                if (chainContract == null) return NotFound(new OperationResponse(false, $"Sidechain {chainName} not found"));
                if (!chainContract.CandidatureTime) return BadRequest(new OperationResponse(false, $"Sidechain not in candidature time"));

                var networkInfo = await _mainchainService.GetInfo();
                var networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);

                var clientInfo = await _mainchainService.RetrieveClientTable(chainName);
                if (clientInfo == null) return NotFound(new OperationResponse(false, $"Sidechain {chainName} client info not found"));

                var contractInfo = await _mainchainService.RetrieveContractInformation(chainName);
                if (contractInfo == null) return NotFound(new OperationResponse(false, $"Sidechain {chainName} contract info not found"));

                var softwareVersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
                var softwareVersion = VersionHelper.ConvertFromVersionString(softwareVersionString);
                var versionInContract = await _mainchainService.RetrieveSidechainNodeVersion(chainName);
                if (versionInContract.SoftwareVersion > softwareVersion)
                    return BadRequest(new OperationResponse(false, $"Sidechain is running version {VersionHelper.ConvertFromVersionInt(versionInContract.SoftwareVersion)} while current node is running version {softwareVersionString}"));

                //if the chain exists in the pool it should mean that he's associated with it
                var chainExistsInPool = _sidechainProducerService.DoesChainExist(chainName);
                if (chainExistsInPool)
                {
                    var sidechainContext = _sidechainProducerService.GetSidechainContext(chainName);

                    //if it's running he should need to do anything because the state manager will decide what to do
                    if (sidechainContext.SidechainStateManager.TaskContainer.IsRunning())
                        return BadRequest(new OperationResponse(false, $"Request to produce sidechain {chainName} previously sent."));
                    //if it's not running, there was a problem and it should be removed from the pool list
                    else
                    {
                        //if chain exists in pool and isn't running, remove it
                        //this also means that there should be remnants of the database
                        _logger.LogDebug($"Removing sidechain {chainName} execution engine");
                        _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(chainName);
                    }
                }

                if((contractInfo.NumberOfFullProducersRequired == 0 && providerType == 3) || (contractInfo.NumberOfHistoryProducersRequired == 0 && providerType == 2) || (contractInfo.NumberOfValidatorProducersRequired == 0 && providerType == 1))
                    return BadRequest(new OperationResponse(false, $"Producer type inserted is not needed in the given sidechain configuration"));
                if (contractInfo.BlockTimeDuration < 60 && networkName == EosNetworkNames.MAINNET)
                    return BadRequest(new OperationResponse(false, $"Sidechain has block time lower than 60 seconds running on mainnet and can not be joined"));

                var candidates = await _mainchainService.RetrieveCandidates(chainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(chainName);
                
                var isPublicKeyAlreadyUsed = producers.Any(p => p.PublicKey == NodeConfigurations.ActivePublicKey) || candidates.Any(c => c.PublicKey == NodeConfigurations.ActivePublicKey);
                if (isPublicKeyAlreadyUsed) return BadRequest(new OperationResponse(false, $"Key {NodeConfigurations.ActivePublicKey} is already being used by another producer or candidate"));

                var isProducerInTable = producers.Any(c => c.Key == NodeConfigurations.AccountName);

                var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(chainName);
                //if the database exists and he's on the producer table, then nothing should be done
                if (chainExistsInDb && isProducerInTable)
                {
                    return BadRequest(new OperationResponse(false, $"{NodeConfigurations.AccountName} is a provider in {chainName}"));
                }
                //if he's not a producer, but is requesting again to be one, and has a database associated, he should delete it first
                else if (chainExistsInDb)
                {
                    return BadRequest(new OperationResponse(false, $"There is a database related to this chain. Please delete it"));
                }


                var accountStake = await _mainchainService.GetAccountStake(chainName, NodeConfigurations.AccountName);
                decimal providerStake = 0;
                if (accountStake != null)
                {
                    var stakeString = accountStake.Stake?.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                    decimal.TryParse(stakeString, out providerStake);
                }
                var minimumProviderState = Math.Round((decimal)contractInfo.Stake / 10000, 4);
                if (minimumProviderState > providerStake + stake)
                {
                    return BadRequest(new OperationResponse(false, $"Minimum provider stake is {minimumProviderState}, currently staked {providerStake} and added {stake} which is not enough. Please stake {minimumProviderState - providerStake}"));
                }

                
                await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(chainName, clientInfo.SidechainCreationTimestamp, false);

                if (stake > 0)
                {
                    var stakeTransaction = await _mainchainService.AddStake(chainName, NodeConfigurations.AccountName, stake.ToString("F4") + " BBT");
                    _logger.LogInformation("Sent stake to contract. Tx = " + stakeTransaction);
                    _logger.LogInformation("Stake inserted = " + stake.ToString("F4") + " BBT");
                }

                await _sidechainProducerService.AddSidechainToProducerAndStartIt(chainName, clientInfo.SidechainCreationTimestamp, providerType, false);

                return Ok(new OperationResponse(true, "Candidature successfully added"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Removes an active candidature to a sidechain production
        /// </summary>
        /// <param name="sidechainName">Account name of the sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Request to leave sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending request</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Removes an active candidature to a sidechain production",
            Description = "If a sidechain is still in the candidature phase and if this node is a candidate to it, this method removes the existing candidature",
            OperationId = "RemoveCandidature"
        )]
        public async Task<ObjectResult> RemoveCandidature(string sidechainName)
        {
            if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            sidechainName = sidechainName.Trim();
            try
            {

                var chainContract = await _mainchainService.RetrieveContractState(sidechainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(sidechainName);
                if (chainContract == null) return NotFound(new OperationResponse(false, $"Sidechain {sidechainName} not found"));
                if (candidatureTable == null) return NotFound(new OperationResponse(false, $"Unable to retrieve {sidechainName} candidature table"));

                var isProducerInCandidature = candidatureTable.Where(m => m.Key == NodeConfigurations.AccountName).Any();

                if (!isProducerInCandidature)
                    return BadRequest(new OperationResponse(false, $"Producer {NodeConfigurations.AccountName} not found in sidechain {sidechainName}"));

                if (!chainContract.CandidatureTime)
                    return BadRequest(new OperationResponse(false, $"Sidechain is not in candidature time so candidature can't be removed"));

                var trx = await _mainchainService.RemoveCandidature(sidechainName, NodeConfigurations.AccountName);
                _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(sidechainName);

                return Ok(new OperationResponse(true, $"Candidature succesfully removed from {sidechainName}. Tx: {trx}"));

            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }


        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract stating that the provider wants to leave this sidechain
        /// </summary>
        /// <param name="sidechainName">Account name of the sidechain</param>
        /// <param name="cleanLocalSidechainData">Indicates if the local data about the sidechain should be removed</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Request to leave sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending request</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract stating that the provider wants to leave this sidechain",
            Description = "The provider uses this service to state that he wants to stop producing for this sidechain, please note that leaving a sidechain will take a full day to take effect",
            OperationId = "RequestToLeaveSidechainProduction"
        )]
        public async Task<ObjectResult> RequestToLeaveSidechainProduction(string sidechainName, bool cleanLocalSidechainData = false)
        {
            //TODO rpinto - to verify that a manual request to leave a sidechain shouldn't delete the database. That has to be done independently

            if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            sidechainName = sidechainName.Trim();

            try
            {

                var chainContract = await _mainchainService.RetrieveContractState(sidechainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(sidechainName);
                var producersTable = await _mainchainService.RetrieveProducersFromTable(sidechainName);
                if (chainContract == null) return NotFound(new OperationResponse(false, $"Sidechain {sidechainName} not found"));
                if (candidatureTable == null) return NotFound(new OperationResponse(false, $"Unable to retrieve {sidechainName} candidature table"));

                var isProducerInCandidature = candidatureTable.Where(m => m.Key == NodeConfigurations.AccountName).Any();
                var isProducerAnActiveProducer = producersTable.Where(m => m.Key == NodeConfigurations.AccountName).Any();

                if (!isProducerInCandidature && !isProducerAnActiveProducer)
                    return BadRequest(new OperationResponse(false, $"Producer {NodeConfigurations.AccountName} not found in sidechain {sidechainName}"));

                _logger.LogDebug($"Sending sidechain exit request for {sidechainName}");
                var trx = await _mainchainService.SidechainExitRequest(sidechainName);


                //TODO rpinto - needs to verify if exist request has been sent successfully



                return Ok(new OperationResponse(true, $"Exit successfully requested for {sidechainName}. This node will automatically leave the sidehain after a full day has passed."));

            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Sends a transaction to BlockBase Token Contract to add stake to a sidechain
        /// </summary>
        /// <param name="sidechainName">Account name of the sidechain</param>
        /// <param name="stake">Stake value to add</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Stake added with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error adding stake</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Token Contract to add stake to a sidechain",
            Description = "The provider uses this service to add stake to a sidechain",
            OperationId = "ProducerAddStake"
        )]
        public async Task<ObjectResult> AddStake(string sidechainName, double stake)
        {
            if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse(false, $"Please provide a valid sidechain name"));
            if (stake <= 0) return BadRequest(new OperationResponse(false, $"Please provide a positive stake value"));
            sidechainName = sidechainName.Trim();

            try
            {
                var chainContract = await _mainchainService.RetrieveContractState(sidechainName);
                if (chainContract == null) return NotFound(new OperationResponse(false, $"Sidechain {sidechainName} not found"));

                var stakeString = $"{stake.ToString("F4")} BBT";
                var trx = await _mainchainService.AddStake(sidechainName, NodeConfigurations.AccountName, stakeString);

                return Ok(new OperationResponse(true, $"Stake successfully added. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Sends a transaction to BlockBase Token Contract to claim back stake from a sidechain
        /// </summary>
        /// <param name="sidechainName">Account name of the sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Stake claimed with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error claiming stake</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Token Contract to claim stake from a sidechain",
            Description = "The provider uses this service to claim stake from a sidechain",
            OperationId = "ProducerClaimStake"
        )]
        public async Task<ObjectResult> ClaimStake(string sidechainName)
        {
            if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse(false, $"Please provide a valid sidechain name"));
            sidechainName = sidechainName.Trim();
            
            try
            {
                var trx = await _mainchainService.ClaimStake(sidechainName, NodeConfigurations.AccountName);

                return Ok(new OperationResponse(true, $"Stake successfully claimed. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }


        /// <summary>
        /// Claims all the rewards that the account of the provider is entitled to
        /// </summary>
        /// <returns>The success of the task</returns>
        /// <response code="200">Rewards claimed with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error claiming rewards</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Claims all the rewards that the account of the provider is entitled to",
            Description = "The provider uses this service to claim all rewards he's entitled to",
            OperationId = "ClaimAllRewards"
        )]
        public async Task<ObjectResult> ClaimAllRewards()
        {

            try
            {
                var accountName = NodeConfigurations.AccountName;
                var rewardTable = await _mainchainService.RetrieveRewardTable(accountName);
                if (rewardTable == null) return NotFound(new OperationResponse(false, $"The reward table for {accountName} was not found"));


                long totalClaimed = 0;
                foreach (var rewardToClaim in rewardTable)
                {
                    if (rewardToClaim.Reward > 0)
                    {
                        try
                        {
                            await _mainchainService.ClaimReward(rewardToClaim.Key, accountName);
                            _logger.LogInformation($"Claimed {Math.Round((double)rewardToClaim.Reward / 10000, 4)} BBT from {rewardToClaim.Key}");
                            totalClaimed += rewardToClaim.Reward;
                        }
                        catch { }
                    }
                }


                return Ok(new OperationResponse(true, $"Successfully claimed {Math.Round((double)totalClaimed / 10000, 4)} BBT"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }


        /// <summary>
        /// Gets information about all currently producing sidechains
        /// </summary>
        /// <returns>Json with information about sidechains currently being produced by node</returns>
        /// <response code="200">Successful get</response>
        /// <response code="500">Error getting information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets information about all currently producing sidechains",
            Description = "The provider uses this request to get information about the sidechains the node is producing",
            OperationId = "GetProducingSidechains"
        )]
        public ObjectResult GetProducingSidechains()
        {
            try
            {
                var poolOfSidechains = _sidechainProducerService.GetSidechainContexts();

                return Ok(new OperationResponse<List<string>>(poolOfSidechains.Select(s => s.SidechainPool.ClientAccountName).ToList(), $"Get producing sidechains successful."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Deletes sidechain data from the database
        /// </summary>
        /// <returns>Status about the deletion process</returns>
        /// <param name="sidechainName">Account name of the sidechain</param>
        /// <param name="force">Use with caution. Stops the local production if its still running and deletes the database.</param>
        /// <response code="200">Successful get</response>
        /// <response code="500">Error getting information</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Deletes sidechain data from the database",
            Description = "The provider uses this request to delete the sidechain data from the database",
            OperationId = "DeleteSidechainFromDatabase"
        )]
        public async Task<ObjectResult> DeleteSidechainFromDatabase(string sidechainName, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            sidechainName = sidechainName.Trim();

            try
            {

                var chainExistsInPool = _sidechainProducerService.DoesChainExist(sidechainName);

                if (chainExistsInPool && !force)
                {
                    return BadRequest(new OperationResponse(false, $"Producer is still working on producing blocks for sidechain {sidechainName}. Consider requesting to leave the sidechain production first. If you're sure, use force=true on the request."));
                }

                if (chainExistsInPool && force)
                {
                    //if chain exists in pool and isn't running, remove it
                    //this also means that there should be remnants of the database
                    _logger.LogDebug($"Removing sidechain {sidechainName} execution engine");
                    _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(sidechainName);
                }


                var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(sidechainName);
                //TODO rpinto - this deletes the whole database - what if a producer leaves production and joins further ahead...?
                if (chainExistsInDb)
                {
                    _logger.LogDebug($"Removing sidechain {sidechainName} data from database");
                    await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(sidechainName);
                }

                var responseMessage = chainExistsInPool && force ? "Successfully stopped chain production. " : "Chain not being produced. ";
                responseMessage += chainExistsInDb ? "Successfully removed chain from database." : "Chain not found in database.";


                return Ok(new OperationResponse(true, responseMessage));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }


        /// <summary>
        /// Gets specific block in sidechain
        /// </summary>
        /// <param name="chainName">Name of the Sidechain</param>
        /// <param name="blockNumber">Number of the block</param>
        /// <returns>The requested block</returns>
        /// <response code="200">Block retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the block</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the block of a given sidechain",
            Description = "Gets the block object requested",
            OperationId = "GetBlock"
        )]
        public async Task<ObjectResult> GetBlock(string chainName, ulong blockNumber)
        {
            if (string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            chainName = chainName.Trim();

            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(chainName);

                if (!doesSidechainExist) return NotFound(new OperationResponse(false, "Sidechain not found"));

                var blockResponse = await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(chainName, blockNumber, blockNumber);
                var block = blockResponse.SingleOrDefault();

                if (block == null) return NotFound(new OperationResponse(false, "Block not found"));

                return Ok(new OperationResponse<Block>(block));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Gets specific transaction in sidechain
        /// </summary>
        /// <param name="chainName">Name of the Sidechain</param>
        /// <param name="transactionNumber">Number of the transaction</param>
        /// <returns>The requested Transaction</returns>
        /// <response code="200">Transaction retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the transaction</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the transaction of a given sidechain",
            Description = "Gets the transaction object requested",
            OperationId = "GetTransaction"
        )]
        public async Task<ObjectResult> GetTransaction(string chainName, ulong transactionNumber)
        {
            if (string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            chainName = chainName.Trim();

            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(chainName);

                if (!doesSidechainExist) return NotFound(new OperationResponse(false, "Sidechain not found"));

                var transaction = await _mongoDbProducerService.GetTransactionBySequenceNumber(chainName, transactionNumber);

                if (transaction == null) return NotFound(new OperationResponse(false, "Transaction not found"));

                return Ok(new OperationResponse<BlockBase.Domain.Blockchain.Transaction>(transaction));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Gets all transactions that haven't been added to a block
        /// </summary>
        /// <param name="chainName">Name of the Sidechain</param>
        /// <returns>The loose transactions</returns>
        /// <response code="200">Transactions retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving transactions</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets all transactions that haven't been added to a block",
            Description = "Gets all transactions that haven't been included in the specified sidechain",
            OperationId = "GetTransactionsInMempool"
        )]
        public async Task<ObjectResult> GetTransactionsInMempool(string chainName)
        {
            if (string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            chainName = chainName.Trim();
            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(chainName);
                if (!doesSidechainExist) return NotFound(new OperationResponse(false, "Sidechain not found"));
                var transactionsInMempool = await _mongoDbProducerService.RetrieveTransactionsInMempool(chainName);

                if (transactionsInMempool == null) return NotFound(new OperationResponse(false, "Sidechain not found."));

                return Ok(new OperationResponse<IEnumerable<BlockBase.Domain.Blockchain.Transaction>>(transactionsInMempool));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Gets the sidechain node software version
        /// </summary>
        /// <param name="chainName">Name of the Sidechain</param>
        /// <returns>Sidechain node software version</returns>
        /// <response code="200">Node software version retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving node software version</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the sidechain node software version",
            Description = "Gets the version the sidechain node software is running. Useful for knowing which version the node should be running in order to send candidature",
            OperationId = "GetSidechainNodeSoftwareVersion"
        )]
        public async Task<ObjectResult> GetSidechainNodeSoftwareVersion(string chainName)
        {
            if (string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse(false, "Please provide a valid sidechain name"));
            chainName = chainName.Trim();
            try
            {
                var versionInContract = await _mainchainService.RetrieveSidechainNodeVersion(chainName);

                return Ok(new OperationResponse(true, $"Sidechain {chainName} is running version {VersionHelper.ConvertFromVersionInt(versionInContract.SoftwareVersion)}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Gets the decrypted node ips this node has access to in a given sidechain
        /// </summary>
        /// <returns>Decrypted node ips</returns>
        /// <response code="200">Decrypted ips retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">No Ips found in table</response>
        /// <response code="402">Producer not found in table</response>
        /// <response code="500">Error retrieving ips</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the decrypted node ips this node has access to in a given sidechain",
            Description = "Gets all the decrypted node ips that are stored in encrypted form in the smart contract tables.",
            OperationId = "GetDecryptedNodeIps"
        )]
        public async Task<ObjectResult> GetDecryptedNodeIps(string sidechainName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse(false, "Please provide a valid account name"));
                sidechainName = sidechainName.Trim();

                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(sidechainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
                var ipAddresses = await _mainchainService.RetrieveIPAddresses(sidechainName);

                if (contractState == null) return BadRequest(new OperationResponse(false, $"Contract state not found for {sidechainName}"));
                if (producers == null) return BadRequest(new OperationResponse(false, $"Producer table not found for {sidechainName}"));
                if (contractInfo == null) return BadRequest(new OperationResponse(false, $"Contract info not found for {sidechainName}"));
                if (ipAddresses == null) return BadRequest(new OperationResponse(false, $"IP Addresses table not found for {sidechainName}"));

                if (!ipAddresses.Any() || ipAddresses.Any(t => !t.EncryptedIPs.Any()))
                    return StatusCode(401, new OperationResponse(false, $"IP Addresses table doesn't have any IPs for {sidechainName}"));

                if (!producers.Any(m => m.Key == NodeConfigurations.AccountName))
                    return StatusCode(402, new OperationResponse(false, $"Producer {NodeConfigurations.AccountName} not found in producers table for {sidechainName}"));

                var ipsToReturn = new Dictionary<string, string>();

                foreach (var ipAddressTable in ipAddresses) ipAddressTable.EncryptedIPs.RemoveAt(ipAddressTable.EncryptedIPs.Count - 1);

                int numberOfIpsToTake = (int)Math.Ceiling(producers.Count() / 4.0);
                var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producers, producers.FindIndex(m => m.Key == NodeConfigurations.AccountName)).Take(numberOfIpsToTake).ToList();

                foreach (var producer in orderedProducersInPool)
                {
                    var producerIndex = orderedProducersInPool.IndexOf(producer);
                    var producerIps = ipAddresses.Where(p => p.Key == producer.Key).FirstOrDefault();

                    var listEncryptedIPEndPoints = producerIps.EncryptedIPs;
                    var encryptedIpEndPoint = listEncryptedIPEndPoints[producerIndex];
                    var producerIp = AssymetricEncryption.DecryptIP(encryptedIpEndPoint, NodeConfigurations.ActivePrivateKey, producer.PublicKey);
                    ipsToReturn.Add(producer.Key, producerIp.ToString());
                }

                return Ok(new OperationResponse<Dictionary<string, string>>(ipsToReturn));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }
    }
}
