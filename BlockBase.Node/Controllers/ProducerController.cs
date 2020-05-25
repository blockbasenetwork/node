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
using BlockBase.Network.Mainchain.Pocos;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Domain.Enums;
using BlockBase.DataPersistence.Utils;

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
        public async Task<ObjectResult> CheckProducerConfig() {
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
            

            try
            {
                var accountInfo = await _mainchainService.GetAccount(NodeConfigurations.AccountName);
                currencyBalance = await _mainchainService.GetCurrencyBalance(NetworkConfigurations.BlockBaseTokenContract, NodeConfigurations.AccountName);
                
                accountDataFetched = true;
                cpuUsed = accountInfo.cpu_limit.used;
                cpuLimit = accountInfo.cpu_limit.max;
                netUsed = accountInfo.net_limit.used;
                netLimit = accountInfo.net_limit.max;
                ramUsed = accountInfo.ram_usage;
                ramLimit = accountInfo.ram_quota;
                
            }
            catch {}
            
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
            catch(Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<dynamic>(e));    
            }
        }

        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract that contains the producer application information for producing the sidechain
        /// </summary>
        /// <param name="chainName">Account name of the sidechain</param>
        /// <param name="workTime">How much time in seconds the producer will produce the sidechain</param>
        /// <param name="producerType">The type of producer the node is going to be for this sidechain</param>
        /// <param name="forceDelete">This parameter is here only to simplify testing purposes. It makes it more easy to restart the whole system and delete previous existing databases</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Candidature sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending candidature</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract that contains the producer application information for producing the sidechain",
            Description = "The producer uses this service to apply to producing a specific sidechain. With this service, they send information about how much time in seconds they are willing to work on that sidechain",
            OperationId = "RequestToProduceSidechain"
        )]
        public async Task<ObjectResult> RequestToProduceSidechain(string chainName, int workTime, int producerType, bool forceDelete = false)
        {
            if (string.IsNullOrEmpty(chainName) || workTime <= 0)
            {
                return BadRequest(new OperationResponse<bool>(new ArgumentException()));
            }
            try
            {
                var sidechainExists = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(chainName);
                var poolOfSidechains = _sidechainProducerService.GetSidechains();
                var chainExists = poolOfSidechains.TryGetValue(chainName, out var existingChain);
                
                if (chainExists && !forceDelete) return BadRequest(new OperationResponse<bool>(new ArgumentException(), "Candidature has already been sent for this Sidechain."));
                if (chainExists && forceDelete) _sidechainProducerService.RemoveSidechainFromProducer(existingChain);
                if (sidechainExists && !forceDelete) return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(new ApplicationException(), "Sidechain not being produced but added in local database, please force delete in order to remove it."));
                if (sidechainExists && forceDelete) await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(chainName);

                await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(chainName);

                var secretHash = HashHelper.Sha256Data(HashHelper.Sha256Data(Encoding.ASCII.GetBytes(NodeConfigurations.SecretPassword)));
                var transaction = await _mainchainService.AddCandidature(chainName, NodeConfigurations.AccountName, workTime, NodeConfigurations.ActivePublicKey, HashHelper.ByteArrayToFormattedHexaString(secretHash), producerType);

                _logger.LogDebug("Sent producer application. Tx = " + transaction);

                var sidechainPool = new SidechainPool(chainName, (ProducerTypeEnum)producerType);

                _sidechainProducerService.AddSidechainToProducer(sidechainPool);

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
        public async Task<ObjectResult> RequestToLeaveSidechainProduction(string sidechainName)
        {
            throw new NotImplementedException();
        }
    }
}
