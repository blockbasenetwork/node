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
        
        public ProducerController(ILogger<ChainController> logger, IOptions<NodeConfigurations> nodeConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService)
        {
            NodeConfigurations = nodeConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;

        }

        /// <summary>
        /// Sends a transaction to blockbase operation contract to submit a candidature for a target sidechain.
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
            Summary = "Sends a transaction to blockbase operation contract to submit a candidature for a target sidechain.",
            Description = "Description here",
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
                if (poolOfSidechains.ContainsKey(chainName)) return BadRequest(new OperationResponse<bool>(new ArgumentException(), "Candidature has already been sent for this Sidechain."));
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

        /// <summary>
        /// Gets the sidechain contract information of a specific account that is started and configured.
        /// </summary>
        /// <param name="chainName">Account name of the sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Contract information obtained with success.</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting the contract information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the sidechain contract information of a specific account that is started and configured.",
            Description = "Description here",
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
        /// Gets the confirmation if the user account has a candidateure in specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <param name="forceDelete">Producer can choose to delete the database with the same account name</param>
        /// <returns> A boolean if the account is candidate in the sidechain</returns>
        /// <response code="200">Information obtained with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting the information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the confirmation if the user account has a candidateure in specific sidechain.",
            Description = "Description here",
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
        /// Gets the current state of a specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <returns>The current state of the contract</returns>
        /// <response code="200">Contract state obtained with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting contract state</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the current state of a specific sidechain.",
            Description = "Description here",
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
        /// Gets the total producers needed for a specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <returns>The number of producers needed for a sidechain.</returns>
        /// <response code="200">Producers needed obtained with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting the producers needed</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the total producers needed for a specific sidechain.",
            Description = "Description here",
            OperationId = "GetTotalProducersNeeded"
        )]
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

        /// <summary>
        /// Gets the total payment to the producers until the request moment of a specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <returns>The total payment of the producers per settlement.</returns>
        /// <response code="200">Chain payment obtained with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting the payments information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the total payment to the producers until the request moment of a specific sidechain.",
            Description = "Description here",
            OperationId = "GetTotalProducerPayment"
        )]
        //TODO Change name to something more intuitive.
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

        /// <summary>
        /// Gets the total number of candidates of a specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the sidechain</param>
        /// <returns>The number of candidates in the sidechain.</returns>
        /// <response code="200">Total producers obtained with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting total candidates information.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the total number of candidates of a specific sidechain.",
            Description = "Description here",
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

        /// <summary>
        /// Gets the payment per block of a specific sidechain.
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <returns>The total payment for the producers.</returns>
        /// <response code="200">Chain payment obtained with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error getting the payment information.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get the payment per block of a specific sidechain.",
            Description = "Description here",
            OperationId = "GetChainPayment"
        )]
        //TODO Change name to something more intuitive.
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
