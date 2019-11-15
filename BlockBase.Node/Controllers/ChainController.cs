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
                var sidechainMaintainer = new SidechainMaintainerManager(
                    new SidechainPool(NodeConfigurations.AccountName),
                    _logger, 
                    _mainchainService);
                
                sidechainMaintainer.Start();

                return Ok(new OperationResponse<bool>(true));
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
