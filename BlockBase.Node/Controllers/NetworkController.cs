

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.SidechainProducer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Domain;
using BlockBase.Utils;

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
        private IMongoDbProducerService _mongoDbProducerService;
        
        public NetworkController(ILogger<NetworkController> logger, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService)
        {
            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
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

                if(contractInfo == null) throw new InvalidOperationException($"{sidechainName} not found");

                var result = new GetSidechainConfigurationModel {
                    account_name = contractInfo.Key,
                    BlocksBetweenSettlement = contractInfo.BlocksBetweenSettlement,
                    BlockTimeDuration = contractInfo.BlockTimeDuration,
                    CandidatureEndDate = DateTimeOffset.FromUnixTimeSeconds(contractInfo.CandidatureEndDate).DateTime,
                    CandidatureTime = contractInfo.CandidatureTime,
                    MaxPaymentPerBlockFullProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockFullProducers/10000,4),
                    MaxPaymentPerBlockHistoryProducers =  Math.Round((decimal)contractInfo.MaxPaymentPerBlockHistoryProducers/10000,4),
                    MaxPaymentPerBlockValidatorProducers =  Math.Round((decimal)contractInfo.MaxPaymentPerBlockValidatorProducers/10000,4),
                    MinPaymentPerBlockFullProducers =  Math.Round((decimal)contractInfo.MinPaymentPerBlockFullProducers/10000,4),
                    MinPaymentPerBlockHistoryProducers =  Math.Round((decimal)contractInfo.MinPaymentPerBlockHistoryProducers/10000,4),
                    MinPaymentPerBlockValidatorProducers =  Math.Round((decimal)contractInfo.MinPaymentPerBlockValidatorProducers/10000,4),
                    Stake =  Math.Round((decimal)contractInfo.Stake/10000,4),
                    NumberOfFullProducersRequired = contractInfo.NumberOfFullProducersRequired,
                    NumberOfHistoryProducersRequired = contractInfo.NumberOfHistoryProducersRequired,
                    NumberOfValidatorProducersRequired = contractInfo.NumberOfValidatorProducersRequired,
                    ReceiveEndDate = contractInfo.ReceiveEndDate,
                    ReceiveTime = contractInfo.ReceiveTime,
                    SecretEndDate = contractInfo.SecretEndDate,
                    SendEndDate = contractInfo.SendEndDate,
                    SendSecretTime = contractInfo.SendSecretTime,
                    SendTime = contractInfo.SendTime,
                    SizeOfBlockInBytes = contractInfo.SizeOfBlockInBytes
                };
                
                return Ok(new OperationResponse<dynamic>(result));
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
        /// <returns>Information about the producer in the sidechain</returns>
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
        public async Task<ObjectResult> GetProducerCandidatureState(string accountName, string sidechainName)
        {
            try
            {
                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(sidechainName);

                if (!contractState.CandidatureTime) return BadRequest(new OperationResponse<bool>(false, "Sidechain not in candidature time"));
                
                var hasProducerApplied = candidatureTable.Select(m => m.Key).Contains(accountName);

                if(!hasProducerApplied) return Ok(new OperationResponse<bool>(hasProducerApplied,$"Producer {accountName} not found"));
                else return Ok(new OperationResponse<bool>(hasProducerApplied,$"Producer {accountName} found"));
                
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
                var candidates = await _mainchainService.RetrieveCandidates(sidechainName);
                var tokenLedger = await _mainchainService.GetAccountStake(sidechainName, sidechainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(sidechainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
                var reservedSeats = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);
                
                var slotsTakenByReservedSeats = 0;
                var fullNumberOfSlotsTakenByReservedSeats = 0;
                var historyNumberOfSlotsTakenByReservedSeats = 0;
                var validatorNumberOfSlotsTakenByReservedSeats = 0;

                foreach(var reservedSeat in reservedSeats) {
                    var producer = producers.Where(o => o.Key == reservedSeat.Key).FirstOrDefault();
                    if(producer != null) {
                        slotsTakenByReservedSeats = slotsTakenByReservedSeats+1;
                        if(producer.ProducerType == 3) fullNumberOfSlotsTakenByReservedSeats = fullNumberOfSlotsTakenByReservedSeats+1;
                        if(producer.ProducerType == 2) historyNumberOfSlotsTakenByReservedSeats = historyNumberOfSlotsTakenByReservedSeats+1;
                        if(producer.ProducerType == 1) validatorNumberOfSlotsTakenByReservedSeats = validatorNumberOfSlotsTakenByReservedSeats+1;
                    } 
                }

                var sidechainState = new SidechainState() {
                   
                    State = contractStates.ConfigTime ? "Configure state" : contractStates.SecretTime ? "Secrect state" : contractStates.IPSendTime ? "Ip Send Time" : contractStates.IPReceiveTime ? "Ip Receive Time" : contractStates.ProductionTime ? "Production" : contractStates.Startchain ? "Startchain" : "No State in chain",
                    StakeDepletionEndDate = await StakeEndTimeCalculationAtMaxPayments(sidechainName),
                    CurrentSidechainStake = tokenLedger.Stake,
                    InProduction = contractStates.ProductionTime,
                    ReservedSeats = new ReservedSeats() {
                        TotalNumber = reservedSeats.Count,
                        SlotsStillAvailable = reservedSeats.Count - slotsTakenByReservedSeats,
                        SlotsTaken = slotsTakenByReservedSeats
                    },
                    FullProducersInfo = new SidechainProducersInfo() {
                        NumberOfProducersRequired = (int) contractInfo.NumberOfFullProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 3).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 3).Count(),
                        NumberOfSlotsTakenByReservedSeats = fullNumberOfSlotsTakenByReservedSeats
                        
                    },
                    HistoryProducersInfo = new SidechainProducersInfo() {
                        NumberOfProducersRequired = (int) contractInfo.NumberOfHistoryProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 2).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 2).Count(),
                        NumberOfSlotsTakenByReservedSeats = historyNumberOfSlotsTakenByReservedSeats
                    },
                    ValidatorProducersInfo = new SidechainProducersInfo() {
                        NumberOfProducersRequired = (int) contractInfo.NumberOfValidatorProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 1).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 1).Count(),
                        NumberOfSlotsTakenByReservedSeats = validatorNumberOfSlotsTakenByReservedSeats
                    }
                };
                return Ok(new OperationResponse<SidechainState>(sidechainState));
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

        /// <summary>
        /// Gets the top 21 producers in the EOS network and their endpoints
        /// </summary>
        /// <returns>The top 21 producers info and endpoints</returns>
        /// <response code="200">Producer information retrieved with success</response>
        /// <response code="500">Error retrieving information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the top 21 producers in the EOS network and their endpoints",
            Description = "Allows node to get a list of producers and their endpoints without the need of knowing a specific endpoing",
            OperationId = "GetTop21ProducersAndEndpoints"
        )]
        public async Task<ObjectResult> GetTop21ProducersAndEndpoints()
        {
            try
            {
                var request = HttpHelper.ComposeWebRequestGet($"https://blockbase.network/api/NodeSupport/GetTop21ProducersAndEndpoints/");
                var json = await HttpHelper.CallWebRequest(request);

                return Ok(new OperationResponse<string>(json));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        private async Task<DateTime> StakeEndTimeCalculationAtMaxPayments(string sidechainName) {
            
            var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
            var sidechainStake = (await _mainchainService.GetAccountStake(sidechainName, sidechainName));
            
            var blocksDividedByTotalNumberOfProducers = contractInfo.BlocksBetweenSettlement / (contractInfo.NumberOfFullProducersRequired + contractInfo.NumberOfHistoryProducersRequired + contractInfo.NumberOfValidatorProducersRequired);
            var fullProducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfFullProducersRequired) * contractInfo.MaxPaymentPerBlockFullProducers;
            var historyroducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfHistoryProducersRequired) * contractInfo.MaxPaymentPerBlockHistoryProducers;
            var validatorProducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfValidatorProducersRequired) * contractInfo.MaxPaymentPerBlockFullProducers;

            var sidechainStakeString = sidechainStake.Stake.Split(" ")[0];
            var sidechainStakeInUnitsString = sidechainStakeString.Split(".")[0] + sidechainStakeString.Split(".")[1];

            var timesThatRequesterCanPaySettlementWithAllProvidersAtMaxPrice = ulong.Parse(sidechainStakeInUnitsString) / ((fullProducerPaymentPerSettlement + historyroducerPaymentPerSettlement + validatorProducerPaymentPerSettlement));
            return DateTime.UtcNow.AddSeconds((contractInfo.BlockTimeDuration * contractInfo.BlocksBetweenSettlement) * timesThatRequesterCanPaySettlementWithAllProvidersAtMaxPrice);
        }
    }
}
