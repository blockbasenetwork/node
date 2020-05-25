

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.SidechainProducer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "networkApi")]
    public class NetworkController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ISidechainProducerService _sidechainProducerService;
        private readonly IMainchainService _mainchainService;
        
        public NetworkController(ILogger<NetworkController> logger, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService)
        {
            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
        }

        /// <summary>
        /// Gets the contract information of a sidechain that is started and configured
        /// </summary>
        /// <param name="sidechainName">Name of the sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Contract information retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the contract information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the contract information of a sidechain that is started and configured",
            Description = "Retrieves relevant information about a sidechain, e.g. payment per block, mininum producer stake to participate, required number of producers, max block size in bytes, etc",
            OperationId = "GetSidechainConfiguration"
        )]
        public async Task<ObjectResult> GetSidechainConfiguration(string sidechainName)
        {
            try
            {
                var clientLedger = await _mainchainService.RetrieveClientTokenLedgerTable(sidechainName);
                ContractInformationTable contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);

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
        /// <param name="accountName">Name of the producer</param>
        /// <param name="sidechainName">Name of the sidechain</param>
        /// <param name="forceDelete">This parameter is here only to simplify testing purposes. It makes it more easy to restart the whole system and delete previous existing databases</param>
        /// <returns> A boolean if the account is candidate in the sidechain</returns>
        /// <response code="200">Information retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving the information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets information about the participation state of the producer on a sidechain",
            Description = "Confirms if the producer has applied successfully to produce a given sidechain",
            OperationId = "GetProducerCandidatureState"
        )]
        //TODO Change name to something more intuitive.
        public async Task<ObjectResult> GetProducerCandidatureState(string accountName, string sidechainName, bool forceDelete = false)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractState(sidechainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(sidechainName);

                if (!contractStates.CandidatureTime) return BadRequest(new OperationResponse<bool>(false, "Sidechain not in candidature time"));
                return Ok(new OperationResponse<bool>(candidatureTable.Select(m => m.Key).Contains(accountName)));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<bool>(e));
            }
        }

        /// <summary>
        /// Gets the current state of a given sidechain
        /// </summary>
        /// <param name="sidechainName">Name of the sidechain</param>
        /// <returns>The current state of the contract</returns>
        /// <response code="200">Contract state retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving contract state</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the current state of a given sidechain",
            Description = "Gets the current state of a given sidechain e.g. has chain started, is in configuration phase, is in candidature phase, is secret sharing phase, etc",
            OperationId = "GetSidechainState"
        )]
        public async Task<ObjectResult> GetSidechainState(string sidechainName)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractState(sidechainName);
                return Ok(new OperationResponse<ContractStateTable>(contractStates));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<ContractStateTable>(e));
            }
        }

        /// <summary>
        /// Gets the total number of current candidates for a given sidechain
        /// </summary>
        /// <param name="sidechainName">Name of the sidechain</param>
        /// <returns>The number of candidates in the sidechain</returns>
        /// <response code="200">Total producers retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving total candidates information.</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the current number of candidates for a given sidechain",
            Description = "Gets the current number of candidates that have applied to produce a given sidechain",
            OperationId = "GetTotalCandidatesForSidechain"
        )]
        public async Task<ObjectResult> GetTotalCandidatesForSidechain(string sidechainName)
        {
            try
            {
                //TODO - this information is not enough as is
                var candidates = await _mainchainService.RetrieveCandidates(sidechainName);
                return Ok(new OperationResponse<int>(candidates.Count));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<int>(e));
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
            try
            {
                var looseTransactionsResponse = await _mongoDbProducerService.RetrieveLastLooseTransactions(chainName);

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
