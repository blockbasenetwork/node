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
using BlockBase.Runtime.SidechainProducer;
using System.Text;
using System.Linq;
using BlockBase.Utils.Crypto;
using BlockBase.DataPersistence.ProducerData;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Domain.Enums;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Blockchain;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "providerApi")]
    public class ProducerController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private NetworkConfigurations NetworkConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private IConnectionsChecker _connectionsChecker;

        public ProducerController(ILogger<ProducerController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, IConnectionsChecker connectionsChecker)
        {
            NodeConfigurations = nodeConfigurations?.Value;
            NetworkConfigurations = networkConfigurations?.Value;

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
                var isMongoLive = await _connectionsChecker.IsAbleToConnectToMongoDb();
                var isPostgresLive = await _connectionsChecker.IsAbleToConnectToPostgres();

                var accountName = NodeConfigurations.AccountName;
                var publicKey = NodeConfigurations.ActivePublicKey;


                bool eosAccountDataFetched = false;
                List<string> currencyBalance = null;
                long cpuUsed = 0;
                long cpuLimit = 0;
                long netUsed = 0;
                long netLimit = 0;
                ulong ramUsed = 0;
                long ramLimit = 0;


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

                }
                catch { }

                var mongoDbConnectionString = NodeConfigurations.MongoDbConnectionString;
                var mongoDbPrefix = NodeConfigurations.MongoDbPrefix;

                var postgresHost = NodeConfigurations.PostgresHost;
                var postgresPort = NodeConfigurations.PostgresPort;
                var postgresUser = NodeConfigurations.PostgresUser;

                return Ok(new OperationResponse<dynamic>(
                    new
                    {
                        accountName,
                        publicKey,
                        eosAccountDataFetched,
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
        /// Sends a transaction to BlockBase Operations Contract that contains the producer application information for producing the sidechain
        /// </summary>
        /// <param name="chainName">Account name of the sidechain</param>
        /// <param name="stake">The amount of BBT that the producer want's to stake</param>
        /// <param name="producerType">The type of producer the node is going to be for this sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Candidature sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="404">Sidechain not found</response>
        /// <response code="500">Error sending candidature</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract that contains the producer application information for producing the sidechain",
            Description = "The producer uses this service to apply to producing a specific sidechain. With this service, they send information about how much time in seconds they are willing to work on that sidechain",
            OperationId = "RequestToProduceSidechain"
        )]
        public async Task<ObjectResult> RequestToProduceSidechain(string chainName, int producerType, decimal stake = 0)
        {

            if(string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse<string>("Please provide a valid sidechain name"));
            if(producerType < 1 || producerType > 3)  return BadRequest(new OperationResponse<string>("Please provide a valid producer type. (1) Validator, (2) History, (3) Full"));
            if(stake < 0) return BadRequest(new OperationResponse<string>("Please provide a non-negative stake value"));
            

            try
            {
                var chainContract = await _mainchainService.RetrieveContractState(chainName);
                if(chainContract == null) return NotFound(new OperationResponse<string>($"Sidechain {chainName} not found"));
                if(!chainContract.CandidatureTime) return BadRequest(new OperationResponse<string>($"Sidechain not in candidature time"));

                
                var poolOfSidechains = _sidechainProducerService.GetSidechains();
                var chainExistsInPool = poolOfSidechains.TryGetValue(chainName, out var existingChain);

                if (chainExistsInPool)
                {
                    if (existingChain.ManagerTask.Task.Status == TaskStatus.Running)
                        return BadRequest(new OperationResponse<bool>(new ArgumentException(), $"Request to produce sidechain {chainName} previously sent."));
                    else
                    {
                        //if chain exists in pool and isn't running, remove it
                        //this also means that there should be remnants of the database
                        _logger.LogDebug($"Removing sidechain {chainName} execution engine");
                        _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(existingChain);
                    }
                }
                
                var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(chainName);
                //TODO rpinto - this deletes the whole database - what if a producer leaves production and joins further ahead...?
                if (chainExistsInDb) 
                {
                    _logger.LogDebug($"Removing sidechain {chainName} data from database");
                    await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(chainName);
                }

                await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(chainName);

                if (stake > 0)
                {
                    var stakeTransaction = await _mainchainService.AddStake(chainName, NodeConfigurations.AccountName, stake.ToString("F4") + " BBT");
                    _logger.LogDebug("Sent stake to contract. Tx = " + stakeTransaction);
                    _logger.LogDebug("Stake inserted = " + stake.ToString("F4") + " BBT");
                }

                var secretHash = HashHelper.Sha256Data(HashHelper.Sha256Data(Encoding.ASCII.GetBytes(NodeConfigurations.SecretPassword)));
                var transaction = await _mainchainService.AddCandidature(chainName, NodeConfigurations.AccountName, NodeConfigurations.ActivePublicKey, HashHelper.ByteArrayToFormattedHexaString(secretHash), producerType);

                _logger.LogDebug("Sent producer application. Tx = " + transaction);

                var sidechainPool = new SidechainPool(chainName, (ProducerTypeEnum)producerType);

                _sidechainProducerService.AddSidechainToProducerAndStartIt(sidechainPool);

                return Ok(new OperationResponse<bool>(true, "Candidature successfully added"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }


        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract stating that the producer wants to leave this sidechain
        /// </summary>
        /// <param name="sidechainName">Account name of the sidechain</param>
        /// <param name="cleanLocalSidechainData">Indicates if the local data about the sidechain should be removed</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Request to leave sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending request</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract stating that the producer wants to leave this sidechain",
            Description = "The producer uses this service to state that he wants to stop producing for this sidechain",
            OperationId = "RequestToLeaveSidechainProduction"
        )]
        public async Task<ObjectResult> RequestToLeaveSidechainProduction(string sidechainName, bool cleanLocalSidechainData = false)
        {
            if(string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse<string>("Please provide a valid sidechain name"));

            try
            {

                var chainContract = await _mainchainService.RetrieveContractState(sidechainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(sidechainName);
                var producersTable = await _mainchainService.RetrieveProducersFromTable(sidechainName);
                if(chainContract == null) return NotFound(new OperationResponse<string>($"Sidechain {sidechainName} not found"));
                if(candidatureTable == null) return NotFound(new OperationResponse<string>($"Unable to retrieve {sidechainName} candidature table"));

                //TODO rpinto - is it a bad request? And why not give info anyway even if not in candidature phase?
                var isProducerInCandidature = candidatureTable.Where(m => m.Key == NodeConfigurations.AccountName).Any();
                var isProducerAnActiveProducer = producersTable.Where(m => m.Key == NodeConfigurations.AccountName).Any();


                if(!isProducerInCandidature && !isProducerAnActiveProducer)
                    return BadRequest(new OperationResponse<string>($"Producer {NodeConfigurations.AccountName} not found in sidechain {sidechainName}"));
                
                _logger.LogDebug($"Sending sidechain exit request for {sidechainName}");
                //TODO rpinto - consider sending this request only if the sidechain is in specific states, like candidature, but especially in production
                //leaving in the middle of the remaining states may impose further problems, right?
                var trx = await _mainchainService.SidechainExitRequest(sidechainName);
                
                var poolOfSidechains = _sidechainProducerService.GetSidechains();
                var chainExistsInPool = poolOfSidechains.TryGetValue(sidechainName, out var existingChain);

                if (chainExistsInPool)
                {
                    //if chain exists in pool and isn't running, remove it
                    //this also means that there should be remnants of the database
                    _logger.LogDebug($"Removing sidechain {sidechainName} execution engine");
                    _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(existingChain);
                }
                
                var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(sidechainName);
                //TODO rpinto - this deletes the whole database - what if a producer leaves production and joins further ahead...?
                if (chainExistsInDb && cleanLocalSidechainData) 
                {
                    _logger.LogDebug($"Removing sidechain {sidechainName} data from database");
                    await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(sidechainName);
                }

                return Ok(new OperationResponse<bool>(true, $"Exit successfully requested. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
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
            Description = "The producer uses this service to add stake to a sidechain",
            OperationId = "ProducerAddStake"
        )]
        public async Task<ObjectResult> AddStake(string sidechainName, double stake)
        {
            if(string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse<string>($"Please provide a valid sidechain name"));
            if(stake <= 0) return BadRequest(new OperationResponse<string>($"Please provide a positive stake value"));

            try
            {
                var chainContract = await _mainchainService.RetrieveContractState(sidechainName);
                if(chainContract == null) return NotFound(new OperationResponse<string>($"Sidechain {sidechainName} not found"));

                var stakeString = $"{stake.ToString("F4")} BBT";
                var trx = await _mainchainService.AddStake(sidechainName, NodeConfigurations.AccountName, stakeString);

                return Ok(new OperationResponse<bool>(true, $"Stake successfully added. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
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
            Description = "The producer uses this service to claim stake from a sidechain",
            OperationId = "ProducerClaimStake"
        )]
        public async Task<ObjectResult> ClaimStake(string sidechainName)
        {
            if(string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse<string>($"Please provide a valid sidechain name"));

            try
            {
                
                //TODO rpinto - uncomment if necessary. Does the contract state still exist if the chain has been deleted?
                // var chainContract = await _mainchainService.RetrieveContractState(sidechainName);
                // if(chainContract == null) return NotFound(new OperationResponse<string>($"Sidechain {sidechainName} not found"));

                var trx = await _mainchainService.ClaimStake(sidechainName, NodeConfigurations.AccountName);

                return Ok(new OperationResponse<bool>(true, $"Stake successfully claimed. Tx = {trx}"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
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
            Description = "The producer uses this request to get information about the sidechains this node is producing",
            OperationId = "GetProducingSidechains"
        )]
        public ObjectResult GetProducingSidechains()
        {
            try
            {
                var poolOfSidechains = _sidechainProducerService.GetSidechains();

                return Ok(new OperationResponse<List<string>>(poolOfSidechains.Keys.ToList(), $"Get producing sidechains successful."));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
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
            Description = "The producer uses this request to delete the sidechain data from the database",
            OperationId = "DeleteSidechainFromDatabase"
        )]
        public async Task<ObjectResult> DeleteSidechainFromDatabase(string sidechainName, bool force = false)
        {
            if(string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse<string>("Please provide a valid sidechain name"));
            try
            {

                var poolOfSidechains = _sidechainProducerService.GetSidechains();
                var chainExistsInPool = poolOfSidechains.TryGetValue(sidechainName, out var existingChain);

                if(chainExistsInPool && !force)
                {
                    return BadRequest(new OperationResponse<string>("Producer is still working on producing blocks for sidechain {sidechainName}. Consider requesting to leave the sidechain production first. If you're sure, use force=true on the request."));
                }

                if (chainExistsInPool && force)
                {
                    //if chain exists in pool and isn't running, remove it
                    //this also means that there should be remnants of the database
                    _logger.LogDebug($"Removing sidechain {sidechainName} execution engine");
                    _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(existingChain);
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


                return Ok(new OperationResponse<bool>(true, responseMessage));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
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
            if(string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse<string>("Please provide a valid sidechain name"));
            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfMaintainedSidechainAlreadyExists(chainName);

                if(!doesSidechainExist) return NotFound(new OperationResponse<string>("Sidechain not found"));

                var blockResponse = await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(chainName, blockNumber, blockNumber);
                var block = blockResponse.SingleOrDefault();

                if (block == null) return NotFound(new OperationResponse<string>("Block not found"));

                return Ok(new OperationResponse<Block>(block));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
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
            if(string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse<string>("Please provide a valid sidechain name"));
            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfMaintainedSidechainAlreadyExists(chainName);

                if(!doesSidechainExist) return NotFound(new OperationResponse<string>("Sidechain not found"));

                var transaction = await _mongoDbProducerService.GetTransactionBySequenceNumber(chainName, transactionNumber);

                if (transaction == null) return NotFound(new OperationResponse<string>("Transaction not found"));

                return Ok(new OperationResponse<BlockBase.Domain.Blockchain.Transaction>(transaction));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        /// <summary>
        /// Gets all saved loosed transactions
        /// </summary>
        /// <param name="chainName">Name of the Sidechain</param>
        /// <returns>The loose transactions</returns>
        /// <response code="200">Transactions retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving transactions</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the loose transactions a given sidechain",
            Description = "Gets all the loose transactions saved to be included in the specified sidechain",
            OperationId = "GetLooseTransactions"
        )]
        public async Task<ObjectResult> GetLooseTransactions(string chainName)
        {
            if(string.IsNullOrWhiteSpace(chainName)) return BadRequest(new OperationResponse<string>("Please provide a valid sidechain name"));
            try
            {
                var doesSidechainExist = await _mongoDbProducerService.CheckIfMaintainedSidechainAlreadyExists(chainName);

                if(!doesSidechainExist) return NotFound(new OperationResponse<string>("Sidechain not found"));


                var looseTransactionsResponse = await _mongoDbProducerService.RetrieveLastLooseTransactions(chainName);

                //TODO rpinto - why is this a BadRequest??
                if (looseTransactionsResponse == null) return BadRequest(new OperationResponse<bool>(new ArgumentException(), "Block not found."));

                return Ok(new OperationResponse<IEnumerable<BlockBase.Domain.Blockchain.Transaction>>(looseTransactionsResponse));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }
    }
}
