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
using BlockBase.Domain.Blockchain;
using BlockBase.Node.Commands.Requester;
using BlockBase.Utils.Services;

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


        public RequesterController(ILogger<RequesterController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, IOptions<RequesterConfigurations> requesterConfigurations, IOptions<ApiSecurityConfigurations> apiSecurityConfigurations, IMainchainService mainchainService, ISidechainMaintainerManager sidechainMaintainerManager, DatabaseKeyManager databaseKeyManager, IConnectionsChecker connectionsChecker, IConnector psqlConnector, ConcurrentVariables concurrentVariables, TransactionsManager transactionSender, IMongoDbRequesterService mongoDbRequesterService)
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
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<dynamic>(e));
            }
        }

        /// <summary>
        /// Returns the current staked balance in the sidechain
        /// </summary>
        /// <returns>Stake balance</returns>
        /// <response code="200">Stake retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving staket</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Retrieves the current staked balance in the sidechain",
            Description = "Used to get the current staked balance in the sidechain",
            OperationId = "CheckCurrentStakeInSidechain()"
        )]
        public async Task<ObjectResult> CheckCurrentStakeInSidechain()
        {
            try
            {
                var sidechainName = NodeConfigurations.AccountName;
                var stakeLedger = await _mainchainService.RetrieveAccountStakedSidechains(sidechainName);
                var stakeRecord = stakeLedger.Where(o => o.Sidechain == sidechainName).FirstOrDefault();
                var stakeToReturn = stakeRecord != null ? stakeRecord.Stake : "0.0000 BBT";

                return Ok(new OperationResponse<string>(stakeToReturn, "Stake retrieved with success"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
            var command = new RequestNewSidechainCommand(_logger, _connector, _mainchainService, NodeConfigurations, RequesterConfigurations);
            command.Stake = stake;
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
                _connector.Setup().Wait();

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

                return Ok(new OperationResponse(true, "Secret set with success"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                if (_sidechainMaintainerManager.IsMaintainerRunning() || _sidechainMaintainerManager.IsProductionRunning())
                {
                    return BadRequest(new OperationResponse(false, $"Sidechain was already running."));
                }

                await _sidechainMaintainerManager.Start();
                return Ok(new OperationResponse(true, "Chain maintenance started."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                    return BadRequest(new OperationResponse(false, $"The sidechain isn't running."));

                await _sidechainMaintainerManager.Pause();

                return Ok(new OperationResponse(true, $"Sidechain maintenance paused."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                if (contractSt == null) return BadRequest(new OperationResponse(false, $"Sidechain {NodeConfigurations.AccountName} not found"));

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

                return Ok(new OperationResponse(true, $"Ended sidechain. Tx: {tx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                return BadRequest(new OperationResponse(false, "The stake must be positive"));
            }
            try
            {
                var stakeString = $"{stake.ToString("F4")} BBT";
                var trx = await _mainchainService.AddStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName, stakeString);

                return Ok(new OperationResponse(true, $"Stake successfully added. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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

                return Ok(new OperationResponse(true, $"Stake successfully claimed. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
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
                if (!_databaseKeyManager.DataSynced) return BadRequest(new OperationResponse(false, "Passwords and main key not set."));
                var queryResults = await _sqlCommandManager.Execute(queryScript);

                return Ok(new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                if (!_databaseKeyManager.DataSynced) return BadRequest(new OperationResponse(false, "Passwords and main key not set."));
                var query = $"USE {sidebarQueryInfo.DatabaseName}; SELECT {sidebarQueryInfo.TableName}.* FROM {sidebarQueryInfo.TableName}";
                if (sidebarQueryInfo.Encrypted) query += " ENCRYPTED";
                query += ";";

                var queryResults = await _sqlCommandManager.Execute(query);

                return Ok(new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                if (!_databaseKeyManager.DataSynced) return BadRequest(new OperationResponse(false, "Passwords and main key not set."));
                var structure = _sqlCommandManager.GetStructure();
                return Ok(new OperationResponse<IList<DatabasePoco>>(structure));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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
                    return BadRequest(new OperationResponse(false, "The sidechain maintenance is running."));
                await _sqlCommandManager.RemoveSidechainDatabasesAndKeys();
                return Ok(new OperationResponse(true, $"Deleted databases and cleared all data."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
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

                if (contractState == null) return BadRequest(new OperationResponse(false, $"Contract state not found for {sidechainName}"));
                if (contractInfo == null) return BadRequest(new OperationResponse(false, $"Contract info not found for {sidechainName}"));
                if (ipAddresses == null) return BadRequest(new OperationResponse(false, $"IP Addresses table not found for {sidechainName}"));

                if (!ipAddresses.Any() || ipAddresses.Any(t => !t.EncryptedIPs.Any()))
                    return StatusCode(401, new OperationResponse(false, $"IP Addresses table doesn't have any IPs for {sidechainName}"));

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
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Removes the given account from the sidechain blacklist
        /// </summary>
        /// <returns>Operation success</returns>
        /// <param name="account">Account name to remove from blacklist</param>
        /// <response code="200">Account removed from blacklist with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error removing account from blacklist</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Removes the given account from the sidechain blacklist",
            Description = "Used to remove from the sidechain blacklist an account that has been previously banned",
            OperationId = "RemoveAccountFromBlacklist"
        )]
        public async Task<ObjectResult> RemoveAccountFromBlacklist(string account)
        {
            if (string.IsNullOrWhiteSpace(account)) return BadRequest(new OperationResponse(false, "Please provide a valid account name"));
            account = account.Trim();
            try
            {
                var sidechainName = NodeConfigurations.AccountName;

                var blacklist = await _mainchainService.RetrieveBlacklistTable(sidechainName);

                if (!blacklist.Any(p => p.Key == account))
                    return BadRequest(new OperationResponse(false, $"Producer {account} isn't in the blacklist for sidechain {sidechainName}"));

                var trx = await _mainchainService.RemoveBlacklistedProducer(sidechainName, account);

                return Ok(new OperationResponse(true, $"Account {account} successfully removed from blacklist"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Gets all the current reserved seats in the sidechain
        /// </summary>
        /// <returns> List of reserved seats </returns>
        /// <response code="200">Reserved seats retrieved with success</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Error getting reserved seats information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets all the current reserved seats in the sidechain",
            Description = "Its used to get all the current account names in the reserved seats of the sidechain.",
            OperationId = "CheckSidechainReservedSeats"
        )]
        public async Task<ObjectResult> CheckSidechainReservedSeats()
        {
            try
            {
                var sidechainName = NodeConfigurations.AccountName;
                var reservedSeatsTable = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                var sidechainStates = await _mainchainService.RetrieveContractState(sidechainName);

                if (sidechainStates == null)
                    return BadRequest(new OperationResponse(false, $"The {sidechainName} sidechain is not created."));


                return Ok(new OperationResponse<List<ReservedSeatsTable>>(reservedSeatsTable));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Adds a list of account names to the sidechain reserved seats
        /// </summary>
        /// <returns>Operation success</returns>
        /// <param name="reservedSeatsToAdd">Accounts names to add to the reserved seats of the sidechain</param>
        /// <response code="200">Accounts added to the sidechain reserved seats with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error adding accounts to the sidechain reserved seats</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Adds a list of account names to the sidechain reserved seats",
            Description = "Used to add accounts to the reserved seat in the sidechain. This way, the inserted accounts are selected first to produce in the sidechain",
            OperationId = "AddReservedSeat"
        )]
        public async Task<ObjectResult> AddReservedSeat(List<string> reservedSeatsToAdd)
        {
            if (reservedSeatsToAdd == null || reservedSeatsToAdd.Count() == 0) return BadRequest(new OperationResponse(false, "Please provide a valid account name"));
            try
            {
                var sidechainName = NodeConfigurations.AccountName;

                var reservedSeatsTable = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                var sidechainStates = await _mainchainService.RetrieveContractState(sidechainName);
                var responseString = "";
                var listToAdd = new List<string>();

                if (sidechainStates == null || sidechainStates.IPReceiveTime || sidechainStates.IPSendTime || sidechainStates.SecretTime || !sidechainStates.Startchain)
                    return BadRequest(new OperationResponse(false, $"The {sidechainName} sidechain is not in the correct state or is not created."));

                foreach (var accountToAdd in reservedSeatsToAdd)
                {
                    if (!reservedSeatsTable.Any(p => p.Key == accountToAdd) && accountToAdd != null && accountToAdd.Length > 0)
                    {
                        responseString += " [" + accountToAdd + "] ";
                        listToAdd.Add(accountToAdd);
                    }
                }

                if (listToAdd.Count() == 0) return BadRequest(new OperationResponse(false, $"None of the accounts inserted are eligible to get added to the sidechain reserved seats."));
                var addReserverSeatTx = await _mainchainService.AddReservedSeats(sidechainName, listToAdd);

                return Ok(new OperationResponse(true, $"Reserved seats successfully added for the accounts{responseString} if they exist in EOSIO network. AddReservedSeats tx: {addReserverSeatTx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Removes the given account from the sidechain reserved seats
        /// </summary>
        /// <returns>Operation success</returns>
        /// <param name="reservedSeatsToRemove">Account names to remove from the sidechain reserved seats</param>
        /// <response code="200">Accounts removed from sidechain reserved seats with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error removing accounts from reserved seats</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Removes the given accoutn from the sidechain reserved seats",
            Description = "Used to remove accounts from the sidechain reserved seats.",
            OperationId = "RemoveReservedSeats"
        )]
        public async Task<ObjectResult> RemoveReservedSeats(List<string> reservedSeatsToRemove)
        {
            if (reservedSeatsToRemove == null || reservedSeatsToRemove.Count() == 0) return BadRequest(new OperationResponse(false, "Please provide a valid account name"));
            try
            {
                var sidechainName = NodeConfigurations.AccountName;
                var reservedSeatsTable = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                var sidechainStates = await _mainchainService.RetrieveContractState(sidechainName);
                var validAccounts = "";
                var seatsToRemove = new List<string>();

                if (sidechainStates == null || sidechainStates.IPReceiveTime || sidechainStates.IPSendTime || sidechainStates.SecretTime || !sidechainStates.Startchain)
                    return BadRequest(new OperationResponse(false, $"The {sidechainName} sidechain is not in the correct state or is not created."));

                foreach (var accountToRemove in reservedSeatsToRemove)
                {
                    if (reservedSeatsTable.Any(p => p.Key == accountToRemove) && accountToRemove.Length >= 1)
                    {
                        seatsToRemove.Add(accountToRemove);
                        validAccounts += "[" + accountToRemove + "] ";
                    }
                }
                if (seatsToRemove.Count() == 0) return BadRequest(new OperationResponse(false, $"None of the accounts inserted are eligible to get removed from the sidechain reserved seats."));
                var removeReservedSeatsTx = await _mainchainService.RemoveReservedSeats(sidechainName, seatsToRemove);

                return Ok(new OperationResponse(true, $"Accounts {validAccounts}successfully removed from the reserved seats of the sidechain. RemoveReservedSeats tx: {removeReservedSeatsTx}."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        /// <summary>
        /// Alter current sidechain configurations
        /// </summary>
        /// <returns>Operation success</returns>
        /// <param name="maxPaymentPerBlockFullProducer">New value for max payment per block for full producers</param>
        /// <param name="minPaymentPerBlockFullProducer">New value for min payment per block for full producers</param>
        /// <param name="maxPaymentPerBlockHistoryProducer">New value for max payment per block for history producers</param>
        /// <param name="minPaymentPerBlockHistoryProducer">New value for min payment per block for history producers</param>
        /// <param name="maxPaymentPerBlockValidatorProducer">New value for max payment per block for validator producers</param>
        /// <param name="minPaymentPerBlockValidatorProducer">New value for min payment per block for validator producers</param>
        /// <param name="minCandidatureStake">New value for minimum stake to enter candidature</param>
        /// <param name="numberOfFullProducersRequired">New value for number of full producers required</param>
        /// <param name="numberOfHistoryProducersRequired">New value for number of history producers required</param>
        /// <param name="numberOfValidatorProducersRequired">New value for number of validator producers required</param>
        /// <param name="blockTimeInSeconds">New value for block time in seconds</param>
        /// <param name="blockSizeInBytes">New value for block size in bytes</param>
        /// <response code="200">Configuration changes sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending configuration changes</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Alter current sidechain configurations",
            Description = "Allows requester to send new configurations to be changed in the sidechain. The changes will take effect one day after inserting them. If sent again before the changes take effect it will override the previous request.",
            OperationId = "ChangeSidechainConfigurations"
        )]
        public async Task<ObjectResult> ChangeSidechainConfigurations(decimal? maxPaymentPerBlockFullProducer, decimal? minPaymentPerBlockFullProducer,
                                                                      decimal? maxPaymentPerBlockHistoryProducer, decimal? minPaymentPerBlockHistoryProducer,
                                                                      decimal? maxPaymentPerBlockValidatorProducer, decimal? minPaymentPerBlockValidatorProducer,
                                                                      decimal? minCandidatureStake, int? numberOfFullProducersRequired,
                                                                      int? numberOfHistoryProducersRequired, int? numberOfValidatorProducersRequired,
                                                                      int? blockTimeInSeconds, long? blockSizeInBytes)
        {
            try
            {
                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(NodeConfigurations.AccountName);
                var networkInfo = await _mainchainService.GetInfo();
                var networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);
                if (contractSt == null) return BadRequest(new OperationResponse(false, $"Sidechain doesn't exist"));
                if (contractSt.ConfigTime || contractSt.SecretTime || contractSt.IPSendTime || contractSt.IPReceiveTime) return BadRequest(new OperationResponse(false, $"Sidechain isn't in a state that allows changing configurations"));

                var configurationChanges = new ChangeConfigurationTable();

                configurationChanges.Key = NodeConfigurations.AccountName;

                configurationChanges.BlockTimeDuration = blockTimeInSeconds != null ? Convert.ToUInt32(blockTimeInSeconds.Value) : contractInfo.BlockTimeDuration;
                configurationChanges.SizeOfBlockInBytes = blockSizeInBytes != null ? Convert.ToUInt64(blockSizeInBytes.Value) : contractInfo.SizeOfBlockInBytes;
                configurationChanges.NumberOfFullProducersRequired = numberOfFullProducersRequired != null ? Convert.ToUInt32(numberOfFullProducersRequired.Value) : contractInfo.NumberOfFullProducersRequired;
                configurationChanges.NumberOfHistoryProducersRequired = numberOfValidatorProducersRequired != null ? Convert.ToUInt32(numberOfValidatorProducersRequired.Value) : contractInfo.NumberOfHistoryProducersRequired;
                configurationChanges.NumberOfValidatorProducersRequired = numberOfValidatorProducersRequired != null ? Convert.ToUInt32(numberOfValidatorProducersRequired.Value) : contractInfo.NumberOfValidatorProducersRequired;
                configurationChanges.MaxPaymentPerBlockFullProducers = maxPaymentPerBlockFullProducer != null ? Convert.ToUInt64(10000 * maxPaymentPerBlockFullProducer.Value) : contractInfo.MaxPaymentPerBlockFullProducers;
                configurationChanges.MaxPaymentPerBlockHistoryProducers = maxPaymentPerBlockHistoryProducer != null ? Convert.ToUInt64(10000 * maxPaymentPerBlockHistoryProducer.Value) : contractInfo.MaxPaymentPerBlockHistoryProducers;
                configurationChanges.MaxPaymentPerBlockValidatorProducers = maxPaymentPerBlockValidatorProducer != null ? Convert.ToUInt64(10000 * maxPaymentPerBlockValidatorProducer.Value) : contractInfo.MaxPaymentPerBlockValidatorProducers;
                configurationChanges.MinPaymentPerBlockFullProducers = minPaymentPerBlockFullProducer != null ? Convert.ToUInt64(10000 * minPaymentPerBlockFullProducer.Value) : contractInfo.MinPaymentPerBlockFullProducers;
                configurationChanges.MinPaymentPerBlockHistoryProducers = minPaymentPerBlockHistoryProducer != null ? Convert.ToUInt64(10000 * minPaymentPerBlockHistoryProducer.Value) : contractInfo.MinPaymentPerBlockHistoryProducers;
                configurationChanges.MinPaymentPerBlockValidatorProducers = minPaymentPerBlockValidatorProducer != null ? Convert.ToUInt64(10000 * minPaymentPerBlockValidatorProducer.Value) : contractInfo.MinPaymentPerBlockValidatorProducers;
                configurationChanges.Stake = minCandidatureStake != null ? Convert.ToUInt64(10000 * minCandidatureStake.Value) : contractInfo.Stake;

                //Check configurations
                if (configurationChanges.SizeOfBlockInBytes <= BlockHeaderSizeConstants.BLOCKHEADER_MAX_SIZE)
                    return BadRequest(new OperationResponse(false, $"Configured block max size is lower than 205 bytes, please increase the size"));
                if (configurationChanges.NumberOfFullProducersRequired + configurationChanges.NumberOfHistoryProducersRequired + configurationChanges.NumberOfValidatorProducersRequired == 0)
                    return BadRequest(new OperationResponse(false, $"Requester configurations need to have at least one provider node requested for sidechain production"));
                if (configurationChanges.BlockTimeDuration < 60 && networkName == EosNetworkNames.MAINNET)
                    return BadRequest(new OperationResponse(false, $"Block time needs to be 60 seconds or higher on Mainnet"));

                var mappedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configurationChanges));

                var alterConfigTx = await _mainchainService.AlterConfigurations(NodeConfigurations.AccountName, mappedConfig);

                return Ok(new OperationResponse(true, $"Configuration changes succesfully sent. The changes will take effect after one day. Tx: {alterConfigTx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        public class SidebarQueryInfo
        {
            public bool Encrypted { get; set; }
            public string DatabaseName { get; set; }
            public string TableName { get; set; }
        }

        private async Task<bool> HasEnoughStakeUntilNextSettlement()
        {
            var accountStake = await _mainchainService.GetAccountStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName);
            if (accountStake == null) return false;

            var maxPaymentPerBlock = new[] { RequesterConfigurations.ValidatorNodes.MaxPaymentPerBlock, RequesterConfigurations.HistoryNodes.MaxPaymentPerBlock, RequesterConfigurations.FullNodes.MaxPaymentPerBlock }.Max();
            var numberOfProducers = RequesterConfigurations.FullNodes.RequiredNumber + RequesterConfigurations.HistoryNodes.RequiredNumber + RequesterConfigurations.ValidatorNodes.RequiredNumber;
            var neededBBT = (numberOfProducers * 5) * maxPaymentPerBlock;
            var neededBBTDecimal = Math.Round((decimal)neededBBT / 10000, 4);

            return (accountStake.Stake >= neededBBTDecimal);
        }
    }
}
