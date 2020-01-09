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
    public class ProducerController : ControllerBase
    {
        private NodeConfigurations NodeConfigurations;
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;

        public ProducerController(ILogger<ChainController> logger, IOptions<NodeConfigurations> nodeConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService)
        {
            NodeConfigurations = nodeConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;

        }

        /// <summary>
        /// Sends candidature to a specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <param name="workTime">Producer working time in the Sidechain in seconds</param>
        /// <param name="forceDelete">Producer can choose to delete the database with the same account name</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Candidature sent with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error sending candidature</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a producer candidature to a sidechain",
            Description = "Requires login to access",
            OperationId = "SendCandidatureToSidechain"
        )]
        public async Task<ObjectResult> SendCandidatureToChain(string chainName, int workTime, bool forceDelete = false)
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
                var transaction = await _mainchainService.AddCandidature(chainName, NodeConfigurations.AccountName, workTime, NodeConfigurations.ActivePublicKey, HashHelper.ByteArrayToFormattedHexaString(secretHash));

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

        [HttpGet]
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

        [HttpGet]
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

        [HttpGet]
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

        [HttpGet]
        public async Task<ObjectResult> GetTotalProducersNeeded(string chainName)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractInformation(chainName);
                return Ok(new OperationResponse<int>((int)contractStates.ProducersNumber));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<int>(e));
            }
        }

        [HttpGet]
        public async Task<ObjectResult> GetTotalProducerPayment(string chainName)
        {
            try
            {
                var blockcount = await _mainchainService.GetBlockCount(chainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(chainName);
                return Ok(new OperationResponse<double>((blockcount.Count * contractInfo.Payment)));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<double>(e));
            }
        }

        [HttpGet]
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

        [HttpGet]
        public async Task<ObjectResult> GetChainPayment(string chainName)
        {
            try
            {
                var contractInfo = await _mainchainService.RetrieveContractInformation(chainName);
                return Ok(new OperationResponse<string>(contractInfo.Payment.ToString()));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }
    }
}
