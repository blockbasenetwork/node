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
using BlockBase.DataPersistence.Utils;
using BlockBase.Runtime.Provider;
using BlockBase.Node.Filters;
using BlockBase.Node.Commands.Provider;
using BlockBase.Domain.Results;
using BlockBase.DataPersistence.Sidechain.Connectors;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "providerApi")]
    [ServiceFilter(typeof(ApiKeyAttribute))]
    public class ProviderController : ControllerBase
    {
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ApiSecurityConfigurations _apiSecurityConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private IConnectionsChecker _connectionsChecker;
        private IConnector _connector;

        public ProviderController(ILogger<ProviderController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, IOptions<ApiSecurityConfigurations> apiSecurityConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, IConnectionsChecker connectionsChecker, IConnector psqlConnector)
        {
            _nodeConfigurations = nodeConfigurations?.Value;
            _networkConfigurations = networkConfigurations?.Value;
            _apiSecurityConfigurations = apiSecurityConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _connectionsChecker = connectionsChecker;
            _connector = psqlConnector;
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
            var command = new CheckProviderConfig(_logger, _mainchainService, _nodeConfigurations, _networkConfigurations, _connectionsChecker);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new RequestToProduceSidechainCommand(_logger, _mainchainService, _nodeConfigurations, _sidechainProducerService, _mongoDbProducerService, chainName, providerType, stake);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);

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
            var command = new RemoveCandidatureCommand(_logger, _mainchainService, _nodeConfigurations, _sidechainProducerService, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new RequestToLeaveSidechainProductionCommand(_logger, _mainchainService, _nodeConfigurations, _mongoDbProducerService, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new ProviderAddStakeCommand(_logger, _mainchainService, _nodeConfigurations, stake, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new ProviderClaimStakeCommand(_logger, _mainchainService, _nodeConfigurations, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new ClaimAllRewardsCommand(_logger, _mainchainService, _nodeConfigurations);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
        public async Task<ObjectResult> GetProducingSidechains()
        {
            var command = new GetProducingSidechainsCommand(_logger, _sidechainProducerService, _mainchainService, _nodeConfigurations);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
        }

        /// <summary>
        /// Gets information about all past sidechains
        /// </summary>
        /// <returns>Json with information about past sidechains produced by node</returns>
        /// <response code="200">Successful get</response>
        /// <response code="500">Error getting information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets information about all past sidechains",
            Description = "The provider uses this request to get information about all the past sidechains it has previously produced for",
            OperationId = "GetPastSidechains"
        )]
        public async Task<ObjectResult> GetPastSidechains()
        {
            try
            {
                var pastSidechains = await _mongoDbProducerService.GetAllPastSidechainsAsync();
                var pastSidechainsResult = new List<PastSidechain>();

                foreach(var sidechain in pastSidechains.Where(s => s.AlreadyLeft))
                {
                    var pastSidechain = new PastSidechain(){
                        Name = sidechain.Sidechain,
                        SidechainCreationDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(sidechain.Timestamp)).DateTime,
                        DateLeft = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(sidechain.DateLeftTimestamp)).DateTime,
                        ReasonLeft = sidechain.ReasonLeft
                    };

                    pastSidechainsResult.Add(pastSidechain);
                }

                return Ok(new OperationResponse<List<PastSidechain>>(pastSidechainsResult, $"Get past sidechains successful."));
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
            var command = new DeleteSidechainFromDatabase(_logger, _sidechainProducerService, _mongoDbProducerService, sidechainName, force, _connector);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetBlockCommand(_logger, _mongoDbProducerService, chainName, blockNumber);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetTransactionCommand(_logger, _mongoDbProducerService, chainName, transactionNumber);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetTransactionsInMempoolCommand(_logger, _mongoDbProducerService, chainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new ProviderGetDecryptedNodeIpsCommand(_logger, _mainchainService, _nodeConfigurations, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
        }
    }
}
