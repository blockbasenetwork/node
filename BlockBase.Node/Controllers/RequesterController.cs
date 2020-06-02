using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Mainchain;
using Newtonsoft.Json;
using BlockBase.Runtime.Network;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.DataProxy.Encryption;
using BlockBase.DataPersistence.Utils;
using System.IO;
using System.Text;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.Domain.Results;
using BlockBase.Domain.Pocos;
using EosSharp.Core.Exceptions;
namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "requesterApi")]
    public class RequesterController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private NetworkConfigurations NetworkConfigurations;
        private RequesterConfigurations RequesterConfigurations;
        private SidechainPhasesTimesConfigurations SidechainPhasesTimesConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private SidechainMaintainerManager _sidechainMaintainerManager;
        private DatabaseKeyManager _databaseKeyManager;
        private SecurityConfigurations _securityConfigurations;
        private IConnectionsChecker _connectionsChecker;
        private SqlCommandManager _sqlCommandManager;

        public RequesterController(ILogger<RequesterController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, IOptions<RequesterConfigurations> requesterConfigurations, IOptions<SidechainPhasesTimesConfigurations> sidechainPhasesTimesConfigurations, IOptions<SecurityConfigurations> securityConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, PeerConnectionsHandler peerConnectionsHandler, SidechainMaintainerManager sidechainMaintainerManager, DatabaseKeyManager databaseKeyManager, IConnectionsChecker connectionsChecker, IConnector psqlConnector, ConcurrentVariables concurrentVariables, TransactionSender transactionSender)
        {
            NodeConfigurations = nodeConfigurations?.Value;
            NetworkConfigurations = networkConfigurations?.Value;
            RequesterConfigurations = requesterConfigurations?.Value;
            SidechainPhasesTimesConfigurations = sidechainPhasesTimesConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _sidechainMaintainerManager = sidechainMaintainerManager;
            _databaseKeyManager = databaseKeyManager;
            _connectionsChecker = connectionsChecker;
            _securityConfigurations = securityConfigurations.Value;
            _databaseKeyManager = databaseKeyManager;
            psqlConnector.Setup().Wait();
            _sidechainMaintainerManager = sidechainMaintainerManager;
            _sqlCommandManager = new SqlCommandManager(new MiddleMan(databaseKeyManager), logger, psqlConnector, concurrentVariables, transactionSender, nodeConfigurations.Value, mongoDbProducerService);
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
                var isMongoLive = await _connectionsChecker.IsAbleToConnectToMongoDb();
                var isPostgresLive = await _connectionsChecker.IsAbleToConnectToPostgres();

                var accountName = NodeConfigurations.AccountName;
                var publicKey = NodeConfigurations.ActivePublicKey;


                bool accountDataFetched = false;
                List<string> currencyBalance = null;
                long cpuUsed = 0;
                long cpuLimit = 0;
                long netUsed = 0;
                long netLimit = 0;
                ulong ramUsed = 0;
                long ramLimit = 0;
                string sidechainState = null;


                try
                {
                    var accountInfo = await _mainchainService.GetAccount(NodeConfigurations.AccountName);
                    currencyBalance = await _mainchainService.GetCurrencyBalance(NetworkConfigurations.BlockBaseTokenContract, NodeConfigurations.AccountName);

                    if(accountInfo == null)
                    {
                        _logger.LogError($"Account {NodeConfigurations.AccountName} not found");
                    }

                    accountDataFetched = true;
                    cpuUsed = accountInfo.cpu_limit.used;
                    cpuLimit = accountInfo.cpu_limit.max;
                    netUsed = accountInfo.net_limit.used;
                    netLimit = accountInfo.net_limit.max;
                    ramUsed = accountInfo.ram_usage;
                    ramLimit = accountInfo.ram_quota;

                    sidechainState = _sidechainMaintainerManager._sidechain.State.ToString();

                }
                catch 
                { 
                    _logger.LogError("Failed to retrieve account info");

                }

                var mongoDbConnectionString = NodeConfigurations.MongoDbConnectionString;
                var mongoDbPrefix = NodeConfigurations.DatabasesPrefix;

                var postgresHost = NodeConfigurations.PostgresHost;
                var postgresPort = NodeConfigurations.PostgresPort;
                var postgresUser = NodeConfigurations.PostgresUser;

                return Ok(new OperationResponse<dynamic>(
                    new
                    {
                        accountName,
                        publicKey,
                        sidechainState,
                        accountDataFetched,
                        currencyBalance,
                        cpuUsed,
                        cpuLimit,
                        netUsed,
                        netLimit,
                        ramUsed,
                        ramLimit,
                        isMongoLive,
                        isPostgresLive,
                        mongoDbConnectionString,
                        mongoDbPrefix,
                        postgresHost,
                        postgresPort,
                        postgresUser
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

                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
                if(contractSt != null) return BadRequest(new OperationResponse<string>($"Sidechain {NodeConfigurations.AccountName} already exists"));

                var configuration = GetSidechainConfigurations();

                if (stake > 0)
                {
                    string stakeToInsert = stake.ToString("F4") + " BBT";
                    var stakeTransaction = await _mainchainService.AddStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName, stakeToInsert);
                    _logger.LogDebug("Stake sent to contract. Tx = " + stakeTransaction);
                    _logger.LogDebug("Stake inserted = " + stakeToInsert);
                }

                //TODO rpinto - if ConfigureChain fails, will StartChain fail if run again, and thus ConfigureChain never be reached?
                var startChainTx = await _mainchainService.StartChain(NodeConfigurations.AccountName, NodeConfigurations.ActivePublicKey);
                var i = 0;

                //TODO rpinto - review this while loop
                while (i < 3)
                {
                    
                    await Task.Delay(1000);

                    try
                    {
                        var configureTx = await _mainchainService.ConfigureChain(NodeConfigurations.AccountName, configuration, RequesterConfigurations.ReservedProducerSeats);
                        return Ok(new OperationResponse<bool>(true, $"Chain successfully created and configured. Start chain tx: {startChainTx}. Configure chain tx: {configureTx}"));
                    }
                    catch(ApiErrorException)
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
        /// Starts the maintenance of the sidechain
        /// </summary>
        /// <returns>The success of starting the sidechain maintenance</returns>
        /// <response code="200">Chain maintenance started with success</response>
        /// <response code="500">Error starting the maintenance of the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Step 2 - Starts the maintenance of the sidechain",
            Description = "The requester uses this service to start the process for producers to participate and build the sidechain",
            OperationId = "RunSidechainMaintenance"
        )]
        public async Task<ObjectResult> RunSidechainMaintenance()
        {
            SecurityConfigurations config;
            try
            {
                if (_securityConfigurations.UseSecurityConfigurations)
                { 
                    _databaseKeyManager.SetInitialSecrets(_securityConfigurations);
                }

                else
                {
                    using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                    {
                        var json = await reader.ReadToEndAsync();
                        config = JsonConvert.DeserializeObject<SecurityConfigurations>(json);
                    }
                    _databaseKeyManager.SetInitialSecrets(config);
                }
                string tx = null;
                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);

                //TODO rpinto - could the contract state be in ConfigTime and CandidatureTime or/and ProductionTime?
                //TODO rpinto - why is the StartCandidatureTime called from outside the SuperMethod?

                if (_sidechainMaintainerManager.TaskContainer == null )
                if (_sidechainMaintainerManager.TaskContainer == null 
                || _sidechainMaintainerManager.TaskContainer.Task.IsCanceled
                || _sidechainMaintainerManager.TaskContainer.Task.IsCompleted
                || _sidechainMaintainerManager.TaskContainer.CancellationTokenSource.IsCancellationRequested)
                    _sidechainMaintainerManager.Start();


                var okMessage = tx != null ? $"Chain maintenance started and start candidature sent: Tx: {tx}" : "Chain maintenance started.";

                return Ok(new OperationResponse<bool>(true, okMessage));
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
                await _sidechainMaintainerManager.EndSidechain();
              
                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
                if(contractSt == null) return BadRequest(new OperationResponse<string>($"Sidechain {NodeConfigurations.AccountName} not found"));

                var tx = await _mainchainService.EndChain(NodeConfigurations.AccountName);
                
                return Ok(new OperationResponse<bool>(true, $"Ended sidechain. Tx: {tx}"));
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
                return BadRequest(new OperationResponse<bool>(new ArgumentException()));
            }
            try
            {
                var stakeString = $"{stake.ToString("F4")} BBT";
                var trx = await _mainchainService.AddStake(NodeConfigurations.AccountName, NodeConfigurations.AccountName, stakeString);

                return Ok(new OperationResponse<bool>(true, $"Stake successfully added. Tx = {trx}"));
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

                return Ok(new OperationResponse<bool>(true, $"Stake successfully claimed. Tx = {trx}"));
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

            configurations.BlocksBetweenSettlement = RequesterConfigurations.NumberOfBlocksBetweenSettlements;
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

            configurations.CandidatureTime = SidechainPhasesTimesConfigurations.CandidaturePhaseDurationInSeconds;
            configurations.SendSecretTime = SidechainPhasesTimesConfigurations.SecretSendingPhaseDurationInSeconds;
            configurations.SendTime = SidechainPhasesTimesConfigurations.IpSendingPhaseDurationInSeconds;
            configurations.ReceiveTime = SidechainPhasesTimesConfigurations.IpRetrievalPhaseDurationInSeconds;

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
                CheckIfSecretIsSet();
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
                CheckIfSecretIsSet();
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
                CheckIfSecretIsSet();
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
                if (!_sidechainMaintainerManager.TaskContainer.CancellationTokenSource.IsCancellationRequested) 
                    return Ok(new OperationResponse<bool>(false, $"You need to end sidechain first."));
                await _sqlCommandManager.RemoveSidechainDatabasesAndKeys();
                return Ok(new OperationResponse<bool>(true, $"Removed data."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        private void CheckIfSecretIsSet()
        {
            if (!_databaseKeyManager.DataSynced)
                throw new Exception("Passwords and main key not set.");

        }
        public class SidebarQueryInfo
        {
            public bool Encrypted { get; set; }
            public string DatabaseName { get; set; }
            public string TableName { get; set; }
        }
    }
}
