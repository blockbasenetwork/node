using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using BlockBase.Network.Mainchain;
using BlockBase.DataPersistence.Data;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Requester;
using Newtonsoft.Json;
using BlockBase.Runtime.Network;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.DataProxy.Encryption;
using BlockBase.DataPersistence.Utils;
using System.IO;
using System.Text;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.Domain.Results;
using BlockBase.Domain.Pocos;
using EosSharp.Core.Exceptions;
using BlockBase.Runtime.Sql;
using System.Linq;
using System.Globalization;
using BlockBase.Node.Filters;
using BlockBase.Utils.Crypto;
using System.Reflection;
using BlockBase.Utils;
using BlockBase.Domain.Eos;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "requesterApi")]
    [ServiceFilter(typeof(ApiKeyAttribute))]
    public class RequesterController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private NetworkConfigurations NetworkConfigurations;
        private RequesterConfigurations RequesterConfigurations;
        private ApiSecurityConfigurations ApiSecurityConfigurations;
        private readonly ILogger _logger;
        private readonly IMainchainService _mainchainService;
        private ISidechainMaintainerManager _sidechainMaintainerManager;
        private DatabaseKeyManager _databaseKeyManager;
        private IConnectionsChecker _connectionsChecker;
        private SqlCommandManager _sqlCommandManager;
        private IConnector _connector;

        private ConcurrentVariables _concurrentVariables;

        
        public RequesterController(ILogger<RequesterController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, IOptions<RequesterConfigurations> requesterConfigurations, IOptions<ApiSecurityConfigurations> apiSecurityConfigurations, IMainchainService mainchainService, ISidechainMaintainerManager sidechainMaintainerManager, DatabaseKeyManager databaseKeyManager, IConnectionsChecker connectionsChecker, IConnector psqlConnector, ConcurrentVariables concurrentVariables, TransactionsHandler transactionSender, IMongoDbRequesterService mongoDbRequesterService)
        {
            NodeConfigurations = nodeConfigurations?.Value;
            NetworkConfigurations = networkConfigurations?.Value;
            RequesterConfigurations = requesterConfigurations?.Value;
            ApiSecurityConfigurations = apiSecurityConfigurations?.Value;

            _logger = logger;
            _mainchainService = mainchainService;
            _sidechainMaintainerManager = sidechainMaintainerManager;
            _databaseKeyManager = databaseKeyManager;
            _connectionsChecker = connectionsChecker;
            _databaseKeyManager = databaseKeyManager;
            _connector = psqlConnector;
            _concurrentVariables = concurrentVariables;
            _sqlCommandManager = new SqlCommandManager(new MiddleMan(databaseKeyManager), logger, psqlConnector, concurrentVariables, transactionSender, nodeConfigurations.Value, mongoDbRequesterService);
        }


        /// <summary>
        /// Checks if the BlockBase node is correctly configured to work as a requester
        /// </summary>
        /// <returns>Information about the node configurations and account state</returns>
        /// <response code="200">Information about the current configuration</response>
        /// <response code="500">Some internal error occurred</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Checks if the BlockBase node is correctly configured",
            Description = "Before starting a node as a requester, the admin should check if everything is correctly configured",
            OperationId = "CheckRequesterConfig"
        )]
        public async Task<ObjectResult> CheckRequesterConfig()
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



                    if(string.IsNullOrWhiteSpace(result))
                        fetchedExternalUtcTimeReference = false;

                    var obj = new { datetime = string.Empty };

                    var jsonResult = JsonConvert.DeserializeObject(result, obj.GetType());

                    string dateTimeToParse = ((dynamic)jsonResult).datetime;
                    DateTime parsedTime;
                    if(!DateTime.TryParse(dateTimeToParse, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsedTime))
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
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<dynamic>(e));
            }
        }

        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract to request a sidechain for configuration
        /// </summary>
        /// <param name="stake">The amount of BBT the requester wants to stake in this sidechain for payment to service providers</param>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Chain started with success</response>
        /// <response code="500">Error starting chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Step 1 - Sends a transaction to BlockBase Operations Contract to request a sidechain for configuration",
            Description = "The requester uses this service to request a new sidechain for storing his databases",
            OperationId = "RequestNewSidechain"
        )]
        public async Task<ObjectResult> RequestNewSidechain(decimal stake = 0)
        {
            try
            {
                if (await _connector.DoesDefaultDatabaseExist())
                    return BadRequest(new OperationResponse<string>(false, "You already have databases associated to this requester node. Clear all of the node associated databases and keys with the command RemoveSidechainDatabasesAndKeys or create a new node with a new database prefix."));

                _connector.Setup().Wait();

                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
                if (contractSt != null) return BadRequest(new OperationResponse<string>(false, $"Sidechain {NodeConfigurations.AccountName} already exists"));



                if (stake > 0)
                {
                    string stakeToInsert = stake.ToString("F4") + " BBT";
                    var stakeTransaction = await _mainchainService.AddStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName, stakeToInsert);
                    _logger.LogInformation("Stake sent to contract. Tx = " + stakeTransaction);
                    _logger.LogInformation("Stake inserted = " + stakeToInsert);
                }

                //TODO rpinto - if ConfigureChain fails, will StartChain fail if run again, and thus ConfigureChain never be reached?
                var startChainTx = await _mainchainService.StartChain(NodeConfigurations.AccountName, NodeConfigurations.ActivePublicKey);
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
                        var configureTx = await _mainchainService.ConfigureChain(NodeConfigurations.AccountName, configuration, RequesterConfigurations.ReservedProducerSeats, minimumSoftwareVersion);
                        return Ok(new OperationResponse<string>(true, $"Chain successfully created and configured. Start chain tx: {startChainTx}. Configure chain tx: {configureTx}"));
                    }
                    catch (ApiErrorException)
                    {
                        i++;
                    }
                }

                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationCanceledException());
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Sets Secret
        /// </summary>
        /// <returns>The success of setting the secret </returns>
        /// <response code="200">Secret set with success</response>
        /// <response code="500">Error starting setting secret</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Step 2 - Sets secret",
            Description = "The requester uses this service to set encrypting key and information",
            OperationId = "SetSecret"
        )]
        public async Task<ObjectResult> SetSecret()
        {
            DatabaseSecurityConfigurations config;
            try
            {
                if (RequesterConfigurations.DatabaseSecurityConfigurations.Use)
                {
                    _databaseKeyManager.SetInitialSecrets(RequesterConfigurations.DatabaseSecurityConfigurations);
                }

                else
                {
                    using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                    {
                        var json = await reader.ReadToEndAsync();
                        config = JsonConvert.DeserializeObject<DatabaseSecurityConfigurations>(json);
                    }
                    _databaseKeyManager.SetInitialSecrets(config);
                }

                return Ok(new OperationResponse<string>(true, "Secret set with success"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Starts the maintenance of the sidechain
        /// </summary>
        /// <returns>The success of starting the sidechain maintenance</returns>
        /// <response code="200">Chain maintenance started with success</response>
        /// <response code="500">Error starting the maintenance of the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Step 3 - Starts the maintenance of the sidechain",
            Description = "The requester uses this service to start the process for producers to participate and build the sidechain",
            OperationId = "RunSidechainMaintenance"
        )]
        public async Task<ObjectResult> RunSidechainMaintenance()
        {
            try
            {
                if (!_sidechainMaintainerManager.IsMaintainerRunning() || !_sidechainMaintainerManager.IsProductionRunning())
                {
                    await _sidechainMaintainerManager.Start();
                    return Ok(new OperationResponse<string>(true, "Chain maintenance started."));
                }

                return BadRequest(new OperationResponse<string>(false, $"Sidechain was already running."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }


        /// <summary>
        /// Pauses sidechain maintenance task
        /// </summary>
        /// <returns>The success of the operation</returns>
        /// <response code="200">Chain paused with success</response>
        /// <response code="500">Error pausing the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Pauses all sidechain state updates",
            Description = "The requester can use this method to temporarily pause the maintenance of the sidechain while still being able to execute queries.",
            OperationId = "PauseSidechain"
        )]
        public async Task<ObjectResult> PauseSidechain()
        {
            try
            {
                if (!_sidechainMaintainerManager.IsMaintainerRunning() && !_sidechainMaintainerManager.IsProductionRunning())
                    return BadRequest(new OperationResponse<string>(false, $"The sidechain isn't running."));

                await _sidechainMaintainerManager.Pause();

                return Ok(new OperationResponse<string>(true, $"Sidechain maintenance paused."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }
        /// <summary>
        /// Sends a transaction to the BlockBase Operations Contract to terminate the sidechain and removes sidechain data
        /// </summary>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Chain terminated with success</response>
        /// <response code="500">Error terminating the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to the BlockBase Operations Contract to terminate the sidechain and removes unsent transactions",
            Description = "The requester uses this service to terminate permanently a given sidechain",
            OperationId = "EndSidechain"
        )]
        public async Task<ObjectResult> EndSidechain()
        {
            try
            {
                await _sidechainMaintainerManager.End();

                //TODO rpinto - should all this functionality below be encapsulated inside the sidechainMaintainerManager?
                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
                if (contractSt == null) return BadRequest(new OperationResponse<string>(false, $"Sidechain {NodeConfigurations.AccountName} not found"));

                var account = await _mainchainService.GetAccount(NodeConfigurations.AccountName);
                var verifyBlockPermission = account.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_BLOCK_PERMISSION).FirstOrDefault();
                var verifyHistoryPermisson = account.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_HISTORY_PERMISSION).FirstOrDefault();
                
                if (verifyBlockPermission != null)
                {
                    try
                    {
                        await _mainchainService.UnlinkAction(NodeConfigurations.AccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
                    }
                    catch (ApiErrorException) 
                    {
                        _logger.LogDebug($"Unlink failed because link does not exist");
                    }
                    await _mainchainService.DeletePermission(NodeConfigurations.AccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
                }

                if (verifyHistoryPermisson != null)
                {
                    try
                    {
                        await _mainchainService.UnlinkAction(NodeConfigurations.AccountName, EosMethodNames.HISTORY_VALIDATE);
                    }
                    catch (ApiErrorException) 
                    {
                        _logger.LogDebug($"Unlink failed because link does not exist");
                    }
                    await _mainchainService.DeletePermission(NodeConfigurations.AccountName, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
                }
            
                var tx = await _mainchainService.EndChain(NodeConfigurations.AccountName);

                _concurrentVariables.Reset();

                return Ok(new OperationResponse<string>(true, $"Ended sidechain. Tx: {tx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }



        /// <summary>
        /// Sends a transaction to BlockBase Token Contract to add sidechain stake
        /// </summary>
        /// <param name="stake">Stake value to add</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Stake added with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error adding stake</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Token Contract to add sidechain stake",
            Description = "The requester uses this service to add sidechain stake",
            OperationId = "RequesterAddStake"
        )]
        public async Task<ObjectResult> AddStake(double stake)
        {
            if (stake <= 0)
            {
                return BadRequest(new OperationResponse<string>(false, "The stake must be positive"));
            }
            try
            {
                var stakeString = $"{stake.ToString("F4")} BBT";
                var trx = await _mainchainService.AddStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName, stakeString);

                return Ok(new OperationResponse<string>(true, $"Stake successfully added. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Sends a transaction to BlockBase Token Contract to claim back sidechain stake
        /// </summary>
        /// <returns>The success of the task</returns>
        /// <response code="200">Stake claimed with success</response>
        /// <response code="500">Error claiming stake</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Token Contract to claim back sidechain stake",
            Description = "The producer uses this service to claim back sidechain stake",
            OperationId = "RequesterClaimStake"
        )]
        public async Task<ObjectResult> ClaimStake()
        {
            try
            {
                var trx = await _mainchainService.ClaimStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName);

                return Ok(new OperationResponse<string>(true, $"Stake successfully claimed. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }


        /// <summary>
        /// Generates a formatted base 32 string to be used as a master key
        /// </summary>
        /// <returns>Formatted base 32 string</returns>
        /// <response code="200">Base 32 string generated with success</response>
        /// <response code="500">Error generating base 32 string</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Returns a newly random generated Master Key",
            Description = "The client can use this method to generate the master key used to encrypt the database data",
            OperationId = "GenerateMasterKey"
        )]
        public ObjectResult GenerateMasterKey()
        {
            try
            {
                var key = KeyAndIVGenerator.CreateRandomKey();

                return Ok(new OperationResponse<string>(key, $"Master key successfully created. Master Key = {key}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        private Dictionary<string, object> GetSidechainConfigurations()
        {
            var configurations = new ContractInformationTable();

            configurations.Key = NodeConfigurations.AccountName;

            configurations.BlockTimeDuration = RequesterConfigurations.BlockTimeInSeconds;
            configurations.SizeOfBlockInBytes = RequesterConfigurations.MaxBlockSizeInBytes;
            configurations.NumberOfFullProducersRequired = RequesterConfigurations.FullNodes.RequiredNumber;
            configurations.NumberOfHistoryProducersRequired = RequesterConfigurations.HistoryNodes.RequiredNumber;
            configurations.NumberOfValidatorProducersRequired = RequesterConfigurations.ValidatorNodes.RequiredNumber;
            configurations.MaxPaymentPerBlockFullProducers = Convert.ToUInt64(10000 * RequesterConfigurations.FullNodes.MaxPaymentPerBlock);
            configurations.MaxPaymentPerBlockHistoryProducers = Convert.ToUInt64(10000 * RequesterConfigurations.HistoryNodes.MaxPaymentPerBlock);
            configurations.MaxPaymentPerBlockValidatorProducers = Convert.ToUInt64(10000 * RequesterConfigurations.ValidatorNodes.MaxPaymentPerBlock);
            configurations.MinPaymentPerBlockFullProducers = Convert.ToUInt64(10000 * RequesterConfigurations.FullNodes.MinPaymentPerBlock);
            configurations.MinPaymentPerBlockHistoryProducers = Convert.ToUInt64(10000 * RequesterConfigurations.HistoryNodes.MinPaymentPerBlock);
            configurations.MinPaymentPerBlockValidatorProducers = Convert.ToUInt64(10000 * RequesterConfigurations.ValidatorNodes.MinPaymentPerBlock);
            configurations.Stake = Convert.ToUInt64(10000 * RequesterConfigurations.MinimumProducerStake);

            configurations.CandidatureTime = RequesterConfigurations.SidechainPhasesTimesConfigurations.CandidaturePhaseDurationInSeconds;
            configurations.SendSecretTime = RequesterConfigurations.SidechainPhasesTimesConfigurations.SecretSendingPhaseDurationInSeconds;
            configurations.SendTime = RequesterConfigurations.SidechainPhasesTimesConfigurations.IpSendingPhaseDurationInSeconds;
            configurations.ReceiveTime = RequesterConfigurations.SidechainPhasesTimesConfigurations.IpRetrievalPhaseDurationInSeconds;

            var mappedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configurations));

            return mappedConfig;
        }
        /// <summary>
        /// Sends a query to be executed
        /// </summary>
        /// <param name="queryScript">The query to execute</param>
        /// <returns> Success or list of results </returns>
        /// <response code="200">Query executed with success</response>
        /// <response code="400">Query invalid</response>
        /// <response code="500">Error executing query</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends query to be executed",
            Description = "The requester uses this service to create databases, update them and delete them",
            OperationId = "ExecuteQuery"
        )]
        public async Task<ObjectResult> ExecuteQuery([FromBody] string queryScript)
        {
            try
            {
                if (!_databaseKeyManager.DataSynced) return BadRequest(new OperationResponse<string>(false, "Passwords and main key not set."));
                var queryResults = await _sqlCommandManager.Execute(queryScript);

                return Ok(new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<QueryResult>>(e));
            }
        }

        /// <summary>
        /// Sends a query to get all the values from a certain table in a certain database
        /// </summary>
        /// <returns> Success or list of results </returns>
        /// <response code="200">Query executed with success</response>
        /// <response code="400">Query invalid</response>
        /// <response code="500">Error executing query</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a query to get all the values from a certain table in a certain database",
            Description = "The requester uses this service to see all values encrypted or not from a certain table",
            OperationId = "GetAllTableValues"
        )]
        public async Task<ObjectResult> GetAllTableValues([FromBody] SidebarQueryInfo sidebarQueryInfo)
        {
            try
            {
                if (!_databaseKeyManager.DataSynced) return BadRequest(new OperationResponse<string>(false, "Passwords and main key not set."));
                var query = $"USE {sidebarQueryInfo.DatabaseName}; SELECT {sidebarQueryInfo.TableName}.* FROM {sidebarQueryInfo.TableName}";
                if (sidebarQueryInfo.Encrypted) query += " ENCRYPTED";
                query += ";";

                var queryResults = await _sqlCommandManager.Execute(query);

                return Ok(new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<QueryResult>>(e));
            }
        }

        /// <summary>
        /// Asks for databases, tables and columns structure
        /// </summary>
        /// <returns> Structure of databases </returns>
        /// <response code="200">Structure retrieved with success</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Error getting structure information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Asks for databases, tables and columns structure",
            Description = "The requester uses this service to know databases structure",
            OperationId = "GetStructure"
        )]
        public ObjectResult GetStructure()
        {
            try
            {
                if (!_databaseKeyManager.DataSynced) return BadRequest(new OperationResponse<string>(false, "Passwords and main key not set."));
                var structure = _sqlCommandManager.GetStructure();
                return Ok(new OperationResponse<IList<DatabasePoco>>(structure));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<DatabasePoco>>(e));
            }
        }

        /// <summary>
        /// Removes keys and postgres databases
        /// </summary>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Data removed with success</response>
        /// <response code="500">Error removing data</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Removes encrypted databases and the keys used to encrypt",
            Description = "The requester should use this  after ending the sidechain, ",
            OperationId = "RemoveSidechainDatabasesAndKeys"
        )]
        public async Task<ObjectResult> RemoveSidechainDatabasesAndKeys()
        {
            try
            {
                if (_sidechainMaintainerManager.IsMaintainerRunning() || _sidechainMaintainerManager.IsProductionRunning())
                    return BadRequest(new OperationResponse<string>(false, "The sidechain maintenance is running."));
                await _sqlCommandManager.RemoveSidechainDatabasesAndKeys();
                return Ok(new OperationResponse<string>(true, $"Deleted databases and cleared all data."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Gets the decrypted node ips in the requester sidechain
        /// </summary>
        /// <returns>Decrypted node ips</returns>
        /// <response code="200">Decrypted ips retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="401">No Ips found in table</response>
        /// <response code="500">Error retrieving ips</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the decrypted node ips in the requester sidechain",
            Description = "Gets all the decrypted node ips that are stored in encrypted form in the smart contract tables",
            OperationId = "GetDecryptedNodeIps"
        )]
        public async Task<ObjectResult> GetDecryptedNodeIps()
        {
            try
            {
                var sidechainName = NodeConfigurations.AccountName;

                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
                var ipAddresses = await _mainchainService.RetrieveIPAddresses(sidechainName);

                if (contractState == null) return BadRequest(new OperationResponse<string>(false, $"Contract state not found for {sidechainName}"));
                if (contractInfo == null) return BadRequest(new OperationResponse<string>(false, $"Contract info not found for {sidechainName}"));
                if (ipAddresses == null) return BadRequest(new OperationResponse<string>(false, $"IP Addresses table not found for {sidechainName}"));

                if (!ipAddresses.Any() || ipAddresses.Any(t => !t.EncryptedIPs.Any()))
                    return StatusCode(401, new OperationResponse<string>(false, $"IP Addresses table doesn't have any IPs for {sidechainName}"));

                var ipsToReturn = new Dictionary<string, string>();

                foreach (var ipAddressTable in ipAddresses)
                {
                    var encryptedIp = ipAddressTable.EncryptedIPs?.LastOrDefault();
                    var decryptedIp = AssymetricEncryption.DecryptIP(encryptedIp, NodeConfigurations.ActivePrivateKey, ipAddressTable.PublicKey);
                    ipsToReturn.Add(ipAddressTable.Key, decryptedIp.ToString());
                }

                return Ok(new OperationResponse<Dictionary<string, string>>(ipsToReturn));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        /// <summary>
        /// Removes the given accoutn from the sidechain blacklist
        /// </summary>
        /// <returns>Operation success</returns>
        /// <param name="account">Account name to remove from blacklist</param>
        /// <response code="200">Accoutn removed from blacklist with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error removing accoutn from blacklist</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Removes the given accoutn from the sidechain blacklist",
            Description = "Used to remove from the sidechain blacklist an account that has been previously banned",
            OperationId = "RemoveAccountFromBlacklist"
        )]
        public async Task<ObjectResult> RemoveAccountFromBlacklist(string account)
        {
            if (string.IsNullOrWhiteSpace(account)) return BadRequest(new OperationResponse<string>(false, "Please provide a valid account name"));
            try
            {
                var sidechainName = NodeConfigurations.AccountName;

                var blacklist = await _mainchainService.RetrieveBlacklistTable(sidechainName);

                if (!blacklist.Any(p => p.Key == account))
                    return BadRequest(new OperationResponse<string>(false, $"Producer {account} isn't in the blacklist for sidechain {sidechainName}"));

                var trx = await _mainchainService.RemoveBlacklistedProducer(sidechainName, account);

                return Ok(new OperationResponse<string>(true, $"Account {account} successfully removed from blacklist"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        public class SidebarQueryInfo
        {
            public bool Encrypted { get; set; }
            public string DatabaseName { get; set; }
            public string TableName { get; set; }
        }
    }
}
