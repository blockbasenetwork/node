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

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "providerApi")]
    public class ProducerController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        
        public ProducerController(ILogger<ProducerController> logger, IOptions<NodeConfigurations> nodeConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService)
        {
            NodeConfigurations = nodeConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;

        }

        /// <summary>
        /// Sends a transaction to BlockBase Operations Contract that contains the producer application information for producing the sidechain
        /// </summary>
        /// <param name="chainName">Account name of the sidechain</param>
        /// <param name="workTime">How much time in seconds the producer will produce the sidechain</param>
        /// <param name="forceDelete">This parameter is here only to simplify testing purposes. It makes it more easy to restart the whole system and delete previous existing databases</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Candidature sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending candidature</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a transaction to BlockBase Operations Contract that contains the producer application information for producing the sidechain",
            Description = "The producer uses this service to apply to producing a specific sidechain. With this service, they send information about how much time in seconds they are willing to work on that sidechain",
            OperationId = "SendCandidatureToChain"
        )]
        public async Task<ObjectResult> SendCandidatureToChain(string chainName, int workTime, int producerType, bool forceDelete = false)
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

                var sidechainPool = new SidechainPool(chainName);

                _sidechainProducerService.AddSidechainToProducer(sidechainPool);

                return Ok(new OperationResponse<bool>(true, "Candidature successfully added"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Gets the contract information of a sidechain that is started and configured
        /// </summary>
        /// <param name="chainName">Name of the sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Contract information retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the contract information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the contract information of a sidechain that is started and configured",
            Description = "Retrieves relevant information about a sidechain, e.g. payment per block, mininum producer stake to participate, required number of producers, max block size in bytes, etc",
            OperationId = "GetContractInfo"
        )]
        public async Task<ObjectResult> GetContractInfo(string chainName)
        {
            try
            {
                var clientLedger = await _mainchainService.RetrieveClientTokenLedgerTable(chainName);
                ContractInformationTable contractInfo = await _mainchainService.RetrieveContractInformation(chainName);

                return Ok(new OperationResponse<ContractInformationTable>(contractInfo));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<ContractInformationTable>(e));
            }
        }

        /// <summary>
        /// Gets information about the participation state of the producer on a sidechain
        /// </summary>
        /// <param name="chainName">Name of the sidechain</param>
        /// <param name="forceDelete">This parameter is here only to simplify testing purposes. It makes it more easy to restart the whole system and delete previous existing databases</param>
        /// <returns> A boolean if the account is candidate in the sidechain</returns>
        /// <response code="200">Information retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets information about the participation state of the producer on a sidechain",
            Description = "Confirms if the producer has applied successfully to produce a given sidechain",
            OperationId = "GetProducerCandidature"
        )]
        //TODO Change name to something more intuitive.
        public async Task<ObjectResult> GetProducerCandidature(string chainName, bool forceDelete = false)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractState(chainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(chainName);

                if (!contractStates.CandidatureTime) return BadRequest(new OperationResponse<bool>(false, "Sidechain not in candidature time!"));
                return Ok(new OperationResponse<bool>(candidatureTable.Select(m => m.Key).Contains(NodeConfigurations.AccountName)));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Gets the current state of a given sidechain
        /// </summary>
        /// <param name="chainName">Name of the sidechain</param>
        /// <returns>The current state of the contract</returns>
        /// <response code="200">Contract state retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving contract state</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the current state of a given sidechain",
            Description = "Gets the current state of a given sidechain e.g. has chain started, is in configuration phase, is in candidature phase, is secret sharing phase, etc",
            OperationId = "GetChainState"
        )]
        public async Task<ObjectResult> GetChainState(string chainName)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractState(chainName);
                return Ok(new OperationResponse<ContractStateTable>(contractStates));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<ContractStateTable>(e));
            }
        }

        /// <summary>
        /// Gets the total producers requested for a given sidechain
        /// </summary>
        /// <param name="chainName">Name of the sidechain</param>
        /// <returns>The number of producers needed for a sidechain</returns>
        /// <response code="200">Producers needed retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the number of producers requested</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the total producers requested for a given sidechain",
            Description = "Gets the number of sidechain producers requested for a given sidechain",
            OperationId = "GetTotalProducersNeeded"
        )]
        public async Task<ObjectResult> GetTotalProducersNeeded(string chainName)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractInformation(chainName);
                var numberOfProducersRequired = contractStates.NumberOfValidatorProducersRequired + contractStates.NumberOfHistoryProducersRequired + contractStates.NumberOfFullProducersRequired;
                return Ok(new OperationResponse<uint>(numberOfProducersRequired));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<int>(e));
            }
        }

        /// <summary>
        /// Gets the total number of current candidates for a given sidechain
        /// </summary>
        /// <param name="chainName">Name of the sidechain</param>
        /// <returns>The number of candidates in the sidechain</returns>
        /// <response code="200">Total producers retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving total candidates information.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the current number of candidates for a given sidechain",
            Description = "Gets the current number of candidates that have applied to produce a given sidechain",
            OperationId = "GetTotalCandidatesInChain"
        )]
        public async Task<ObjectResult> GetTotalCandidatesInChain(string chainName)
        {
            try
            {
                var candidates = await _mainchainService.RetrieveCandidates(chainName);
                return Ok(new OperationResponse<int>(candidates.Count));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<int>(e));
            }
        }
    }
}
