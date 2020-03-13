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
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Mainchain;
using Newtonsoft.Json;
using BlockBase.Runtime.Network;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Domain.Blockchain;
using System.Linq;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "requesterApi")]
    public class ChainController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private NetworkConfigurations NetworkConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private PeerConnectionsHandler _peerConnectionsHandler;

        public ChainController(ILogger<ChainController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, PeerConnectionsHandler peerConnectionsHandler)
        {
            NodeConfigurations = nodeConfigurations?.Value;
            NetworkConfigurations = networkConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract to request a sidechain for configuration
        /// </summary>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Chain started with success</response>
        /// <response code="500">Error starting chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract to request a sidechain for configuration",
            Description = "The requester uses this service to request a new sidechain for storing his databases",
            OperationId = "StartChain"
        )]
        public async Task<ObjectResult> StartChain()
        {
            try
            {
                var tx = await _mainchainService.StartChain(NodeConfigurations.AccountName, NodeConfigurations.ActivePublicKey);

                return Ok(new OperationResponse<bool>(true, $"Chain successfully created. Tx: {tx}"));
            }
            catch(Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }
        
        /// <summary>
        /// Sends a transaction to Blockbase Operations Contract with the configuration requested for the sidechain
        /// </summary>
        /// <param name="configuration">The sidechain configuration</param>
        /// <returns>The success of the configuration</returns>
        /// <response code="200">Chain configured with success</response>
        /// <response code="400">Configuration parameters invalid</response>
        /// <response code="500">Error configurating the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract with the configuration requested for the sidechain",
            Description = "The requester uses this service to configure the requirements for the sidechain and for producers participation",
            OperationId = "ConfigureChain"
        )]
        public async Task<ObjectResult> ConfigureChain([FromBody]ContractInformationTable configuration)
        {
            try
            {
                var mappedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configuration));
                var tx = await _mainchainService.ConfigureChain(NodeConfigurations.AccountName, mappedConfig);

                return Ok(new OperationResponse<bool>(true, $"Chain configuration successfully sent. Tx: {tx}"));
            }
            catch(Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Sends a transaction to the BlockBase Operations Contract to terminate the sidechain
        /// </summary>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Chain terminated with success</response>
        /// <response code="500">Error terminating the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to the BlockBase Operations Contract to terminate the sidechain",
            Description = "The requester uses this service to terminate a given sidechain",
            OperationId = "EndChain"
        )]
        public async Task<ObjectResult> EndChain()
        {
            try
            {
                var tx = await _mainchainService.EndChain(NodeConfigurations.AccountName);

                return Ok(new OperationResponse<bool>(true, $"Ended chain. Tx: {tx}"));
            }
            catch(Exception e)
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
            Summary = "Starts the maintenance of the sidechain",
            Description = "The requester uses this service to start the maintenance of the sidechain",
            OperationId = "StartChainMaintenance"
        )]
        public async Task<ObjectResult> StartChainMaintenance()
        {
            try
            {
                string tx = null;
                var contractSt = await _mainchainService.RetrieveContractState(NodeConfigurations.AccountName);
                
                if (!contractSt.CandidatureTime && !contractSt.ProductionTime) tx = await _mainchainService.StartCandidatureTime(NodeConfigurations.AccountName);

                var sidechainMaintainer = new SidechainMaintainerManager(
                    new SidechainPool(NodeConfigurations.AccountName),
                    _logger, 
                    _mainchainService,
                    NodeConfigurations, _peerConnectionsHandler);
                
                sidechainMaintainer.Start();

                var okMessage = tx != null ? $"Chain maintenance started and start candidature sent: Tx: {tx}" : "Chain maintenance started.";

                return Ok(new OperationResponse<bool>(true, okMessage));
            }
            catch(Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }
        /// <summary>
        /// Terminates the sidechain maintenance
        /// </summary>
        /// <returns>The success of terminating the sidechain maintenance</returns>
        /// <response code="200">Chain maintenance terminated with success</response>
        /// <response code="500">Error terminating the chain maintenance</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Terminates the sidechain maintenance",
            Description = "The requester uses this service to end the maintenance of the sidechain",
            OperationId = "EndChainMaintenance"
        )]
        public async Task<ObjectResult> EndChainMaintenance()
        {
            return NotFound(new NotImplementedException());
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
            try
            {
                var blockResponse = await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(chainName, blockNumber, blockNumber);
                var block = blockResponse.FirstOrDefault();

                if (block == null) return BadRequest(new OperationResponse<bool>(new ArgumentException(), "Block not found."));

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
        /// <response code="500">Error retrieving the block</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the transaction of a given sidechain",
            Description = "Gets the transaction object requested",
            OperationId = "GetTransaction"
        )]
        public async Task<ObjectResult> GetTransaction(string chainName, ulong transactionNumber)
        {
            try
            {
                var transactionsResponse = await _mongoDbProducerService.GetTransactionBySequenceNumber(chainName, transactionNumber);
                var transaction = transactionsResponse.FirstOrDefault();

                if (transaction == null) return BadRequest(new OperationResponse<bool>(new ArgumentException(), "Block not found."));

                return Ok(new OperationResponse<Transaction>(transaction));
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
            try
            {
                var looseTransactionsResponse = await _mongoDbProducerService.RetrieveLastLooseTransactions(chainName);

                if (looseTransactionsResponse == null) return BadRequest(new OperationResponse<bool>(new ArgumentException(), "Block not found."));

                return Ok(new OperationResponse<IEnumerable<Transaction>>(looseTransactionsResponse));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }
    }
}
