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

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
        public async Task<ObjectResult> StartChainMaintainance()
        {
            try
            {
                var tx = await _mainchainService.StartCandidatureTime(NodeConfigurations.AccountName);

                var sidechainMaintainer = new SidechainMaintainerManager(
                    new SidechainPool(NodeConfigurations.AccountName),
                    _logger, 
                    _mainchainService,
                    NodeConfigurations, _peerConnectionsHandler);
                
                sidechainMaintainer.Start();

                return Ok(new OperationResponse<bool>(true, $"Chain maintainance started and start candidature sent: Tx: {tx}"));
            }
            catch(Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        [HttpPost]
        public async Task<ObjectResult> EndChainMaintainance()
        {
            return NotFound(new NotImplementedException());
        }
    }
}
