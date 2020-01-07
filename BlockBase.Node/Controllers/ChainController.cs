using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BlockBase.Utils;
using System.Net;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using BlockBase.Network.Mainchain;
using BlockBase.Utils.Operation;
using BlockBase.Network.Sidechain;
using BlockBase.Domain.Enums;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Domain.Eos;
using System.Text;
using BlockBase.Utils.Crypto;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
using BlockBase.Network.Mainchain.Pocos;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Runtime.Mainchain;
using Newtonsoft.Json;

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

        public ChainController(ILogger<ChainController> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService)
        {
            NodeConfigurations = nodeConfigurations?.Value;
            NetworkConfigurations = networkConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
        }

        /// <summary>
        /// Sends a transaction to blockbase operation contract to request the sidechain and enable further configuration.
        /// </summary>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Chain started with success</response>
        /// <response code="500">Error starting chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to blockbase operation contract to request a sidechain and enable further configuration.",
            Description = "Description here",
            OperationId = "Startchain"
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
        /// Sends a transaction to blockbase operation contract to configure the requested sidechain.
        /// </summary>
        /// <param name="configuration">Configurations of the requested sidechain</param>
        /// <returns>The success of the configuration</returns>
        /// <response code="200">Chain configured with success.</response>
        /// <response code="400">Configuration parameters invalid.</response>
        /// <response code="500">Error configurating the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to blockbase operation contract to configure the requested sidechain.",
            Description = "Description here",
            OperationId = "Configurechain"
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
        /// Sends a transaction to the blockbase operation contract to end the requested sidechain.
        /// </summary>
        /// <returns>The success of the transaction</returns>
        /// <response code="200">Chain ended with success</response>
        /// <response code="500">Error ending the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to the blockbase operation contract to end the requested sidechain.",
            Description = "Description here",
            OperationId = "Endchain"
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
        /// Sends the request to start sidechain maintainance.
        /// </summary>
        /// <returns>The success of the task</returns>
        /// <response code="200">Chain maintaince started with success</response>
        /// <response code="500">Error starting maintaince of the chain</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends the request to start sidechain maintainance.",
            Description = "Description here",
            OperationId = "StartChainMaintainance"
        )]
        public async Task<ObjectResult> StartChainMaintainance()
        {
            try
            {
                var tx = await _mainchainService.StartCandidatureTime(NodeConfigurations.AccountName);

                var sidechainMaintainer = new SidechainMaintainerManager(
                    new SidechainPool(NodeConfigurations.AccountName),
                    _logger, 
                    _mainchainService);
                
                sidechainMaintainer.Start();

                return Ok(new OperationResponse<bool>(true, $"Chain maintainance started and start candidature sent: Tx: {tx}"));
            }
            catch(Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }
        /// <summary>
        /// Sends the request to end the sidechain maintainance.
        /// </summary>
        /// <returns>The success in ending chain maintainance</returns>
        /// <response code="200">Chain maintainance ended with success</response>
        /// <response code="500">Error ending the chain maintainance</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends the request to end the sidechain maintainance.",
            Description = "Description here",
            OperationId = "EndChainMaintainance"
        )]
        public async Task<ObjectResult> EndChainMaintainance()
        {
            return NotFound(new NotImplementedException());
        }
    }
}
