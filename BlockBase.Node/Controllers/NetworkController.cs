

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using BlockBase.Domain;
using BlockBase.Utils;
using Newtonsoft.Json;
using BlockBase.Domain.Results;
using BlockBase.Domain.Enums;
using BlockBase.Runtime.Network;
using BlockBase.Node.Filters;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "networkApi")]
    [ServiceFilter(typeof(ApiKeyAttribute))]
    public class NetworkController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IMainchainService _mainchainService;

        private readonly TcpConnectionTester _tcpConnectionTester;

        public NetworkController(ILogger<NetworkController> logger, IMainchainService mainchainService, TcpConnectionTester tcpConnectionTester)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _tcpConnectionTester = tcpConnectionTester;
        }

        /// <summary>
        /// Gets the contract information of a sidechain that is started and configured
        /// </summary>
        /// <param name="sidechainName">Name of the sidechain</param>
        /// <returns>The success of the task</returns>
        /// <response code="200">Contract information retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="404">Contract information not found</response>
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
                if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest(new OperationResponse<string>(false, "Please provide a sidechain name."));
                ContractInformationTable contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);

                if (contractInfo == null) return NotFound(new OperationResponse<string>(false, $"Sidechain {sidechainName} configuration not found"));

                var result = new GetSidechainConfigurationModel
                {
                    account_name = contractInfo.Key,
                    BlocksBetweenSettlement = contractInfo.BlocksBetweenSettlement,
                    BlockTimeDuration = contractInfo.BlockTimeDuration,
                    CandidatureEndDate = DateTimeOffset.FromUnixTimeSeconds(contractInfo.CandidatureEndDate).DateTime,
                    CandidatureTime = contractInfo.CandidatureTime,
                    MaxPaymentPerBlockFullProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockFullProducers / 10000, 4),
                    MaxPaymentPerBlockHistoryProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockHistoryProducers / 10000, 4),
                    MaxPaymentPerBlockValidatorProducers = Math.Round((decimal)contractInfo.MaxPaymentPerBlockValidatorProducers / 10000, 4),
                    MinPaymentPerBlockFullProducers = Math.Round((decimal)contractInfo.MinPaymentPerBlockFullProducers / 10000, 4),
                    MinPaymentPerBlockHistoryProducers = Math.Round((decimal)contractInfo.MinPaymentPerBlockHistoryProducers / 10000, 4),
                    MinPaymentPerBlockValidatorProducers = Math.Round((decimal)contractInfo.MinPaymentPerBlockValidatorProducers / 10000, 4),
                    Stake = Math.Round((decimal)contractInfo.Stake / 10000, 4),
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
        /// <response code="404">Unable to retrieve sidechain data</response>
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
                if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(sidechainName))
                {
                    return BadRequest($"Please provide and producer account name and a sidechain name");
                }

                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(sidechainName);
                var producerTable = await _mainchainService.RetrieveProducersFromTable(sidechainName);

                if (contractState == null) return NotFound($"Unable to retrieve {sidechainName} contract state");
                if (candidatureTable == null && producerTable == null) return NotFound($"Unable to retrieve {sidechainName} candidature and production table");

                if (candidatureTable != null && candidatureTable.Where(m => m.Key == accountName).Any())
                    return Ok(new OperationResponse<bool>(false, $"Account {accountName} has applied for {sidechainName}"));

                if (producerTable != null && producerTable.Where(m => m.Key == accountName).Any())
                    return Ok(new OperationResponse<bool>(false, $"Account {accountName} is producing for {sidechainName}"));

                return Ok(new OperationResponse<bool>(false, $"Producer {accountName} not found"));
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
        /// <response code="404">Sidechain state not found</response>
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
                if (string.IsNullOrWhiteSpace(sidechainName)) return BadRequest("Please provide a valid sidechain name");

                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                var candidates = await _mainchainService.RetrieveCandidates(sidechainName);
                var tokenLedger = await _mainchainService.GetAccountStake(sidechainName, sidechainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(sidechainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
                var reservedSeats = await _mainchainService.RetrieveReservedSeatsTable(sidechainName);

                if (contractState == null) return BadRequest($"Contract state not found for {sidechainName}");
                if (candidates == null) return BadRequest($"Candidate table not found for {sidechainName}");
                if (tokenLedger == null) return BadRequest($"Token ledger table not found for {sidechainName}");
                if (producers == null) return BadRequest($"Producer table not found for {sidechainName}");
                if (contractInfo == null) return BadRequest($"Contract info not found for {sidechainName}");
                if (reservedSeats == null) return BadRequest($"Reserved seats table not found for {sidechainName}");

                var slotsTakenByReservedSeats = 0;
                var fullNumberOfSlotsTakenByReservedSeats = 0;
                var historyNumberOfSlotsTakenByReservedSeats = 0;
                var validatorNumberOfSlotsTakenByReservedSeats = 0;

                foreach (var reservedSeatKey in reservedSeats.Select(r => r.Key).Distinct())
                {
                    var producer = producers.Where(o => o.Key == reservedSeatKey).SingleOrDefault();
                    if (producer != null)
                    {
                        slotsTakenByReservedSeats++;
                        if (producer.ProducerType == 3) fullNumberOfSlotsTakenByReservedSeats++;
                        if (producer.ProducerType == 2) historyNumberOfSlotsTakenByReservedSeats++;
                        if (producer.ProducerType == 1) validatorNumberOfSlotsTakenByReservedSeats++;
                    }
                }

                var sidechainState = new SidechainState()
                {

                    State = contractState.ConfigTime ? "Configure state" : contractState.SecretTime ? "Secrect state" : contractState.IPSendTime ? "Ip Send Time" : contractState.IPReceiveTime ? "Ip Receive Time" : contractState.ProductionTime ? "Production" : contractState.Startchain ? "Startchain" : "No State in chain",
                    StakeDepletionEndDate = StakeEndTimeCalculationAtMaxPayments(contractInfo, tokenLedger),
                    CurrentRequesterStake = tokenLedger.Stake,
                    InProduction = contractState.ProductionTime,
                    ReservedSeats = new ReservedSeats()
                    {
                        TotalNumber = reservedSeats.Count,
                        SlotsStillAvailable = reservedSeats.Count - slotsTakenByReservedSeats,
                        SlotsTaken = slotsTakenByReservedSeats
                    },
                    FullProducersInfo = new SidechainProducersInfo()
                    {
                        NumberOfProducersRequired = (int)contractInfo.NumberOfFullProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 3).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 3).Count(),
                        NumberOfSlotsTakenByReservedSeats = fullNumberOfSlotsTakenByReservedSeats

                    },
                    HistoryProducersInfo = new SidechainProducersInfo()
                    {
                        NumberOfProducersRequired = (int)contractInfo.NumberOfHistoryProducersRequired,
                        NumberOfProducersInChain = producers.Where(o => o.ProducerType == 2).Count(),
                        CandidatesWaitingForSeat = candidates.Where(o => o.ProducerType == 2).Count(),
                        NumberOfSlotsTakenByReservedSeats = historyNumberOfSlotsTakenByReservedSeats
                    },
                    ValidatorProducersInfo = new SidechainProducersInfo()
                    {
                        NumberOfProducersRequired = (int)contractInfo.NumberOfValidatorProducersRequired,
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
        /// All the stake information for each given sidechain the node has stake
        /// </summary>
        /// <returns>Stake information</returns>
        /// <response code="200">Stake information retrieved with success</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Error retrieving stake information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets all stake information of the node account",
            Description = "Gets all the node account staked tokens and on witch sidechain the tokens are staked.",
            OperationId = "GetStakedSidechains"
        )]
        public async Task<ObjectResult> GetAccountStakeRecords(string accountName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accountName)) return BadRequest(new OperationResponse<string>(false, "Please provide a valid account name"));

                var stakeTable = await _mainchainService.RetrieveAccountStakedSidechains(accountName);

                return Ok(stakeTable);
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        /// <summary>
        /// Gets the top producers in the EOS Mainnet and their endpoints
        /// </summary>
        /// <returns>The top producers info and endpoints</returns>
        /// <response code="200">Producer information retrieved with success</response>
        /// <response code="404">Unable to retrieve producers</response>
        /// <response code="500">Error retrieving information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the top producers in the EOS Mainnet and their endpoints",
            Description = "Allows node to get a list of producers and their endpoints without the need of knowing a specific endpoing",
            OperationId = "GetTopProducersAndEndpoints"
        )]
        public async Task<ObjectResult> GetTopProducersAndEndpoints()
        {
            try
            {
                var request = HttpHelper.ComposeWebRequestGet($"https://blockbase.network/api/NodeSupport/GetTop21ProducersAndEndpoints/");
                var json = await HttpHelper.CallWebRequest(request);
                var topProducers = JsonConvert.DeserializeObject<List<TopProducerEndpoint>>(json);

                //TODO rpinto - Nice implementation - should be done periodically though, and not on request
                var topProducersEndpointResponse = await ConvertToAndMeasureTopProducerEndpointResponse(topProducers.Take(10).ToList());

                return Ok(new OperationResponse<List<TopProducerEndpointResponse>>(topProducersEndpointResponse));
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return NotFound(new OperationResponse<string>(false, "Unable to retrieve the list of producers"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        /// <summary>
        /// Gets all the sidechains currently known
        /// </summary>
        /// <param name="network">The network you want to querie for the known tracker sidechains</param>
        /// <returns>The top producers info and endpoints</returns>
        /// <response code="200">Producer information retrieved with success</response>
        /// <response code="404">Unable to retrieve producers</response>
        /// <response code="500">Error retrieving information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets all the sidechains currently known",
            Description = "Allows node to get a list of sidechains currently known from the blockbase tracker, where is possible to filter by network",
            OperationId = "GetTopProducersAndEndpoints"
        )]
        public async Task<ObjectResult> GetAllBlockbaseSidechains(NetworkType network = NetworkType.All)
        {
            try
            {
                var request = HttpHelper.ComposeWebRequestGet($"https://blockbase.network/api/NodeSupport/GetAllTrackerSidechains?network={network.ToString()}");
                var json = await HttpHelper.CallWebRequest(request);
                var trackerSidechains = JsonConvert.DeserializeObject<List<TrackerSidechain>>(json);

                return Ok(new OperationResponse<List<TrackerSidechain>>(trackerSidechains));
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return NotFound(new OperationResponse<string>(false, "Unable to retrieve the list of sidechains"));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }


        /// <summary>
        /// Gets the current list of unclaimed rewards for a given provider
        /// </summary>
        /// <param name="accountName">The name of the provider</param>
        /// <returns>The current list of unclaimed rewards</returns>
        /// <response code="200">Information was retrieved successfully</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Internal error</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the current list of unclaimed rewards for a given provider",
            Description = "Gets the current list of rewards for a given provider.",
            OperationId = "GetCurrentUnclaimedRewards"
        )]
        public async Task<ObjectResult> GetCurrentUnclaimedRewards(string accountName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accountName)) return BadRequest(new OperationResponse<string>(false, "Please provide a valid account name"));

                var rewardTable = await _mainchainService.RetrieveRewardTable(accountName);
                if (rewardTable == null) return NotFound(new OperationResponse<string>(false, $"The reward table for {accountName} was not found"));



                return Ok(new OperationResponse<List<(string provider, string reward)>>(rewardTable.Select(r => (r.Key, $"{Math.Round((double)r.Reward / 10000, 4)} BBT")).ToList()));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }


        /// <summary>
        /// Tries to establish a connection to another node. Lasts for 20 seconds
        /// </summary>
        /// <param name="ipAddress">The public IP address of the other node</param>
        /// <param name="port">The port of the other node</param>
        /// <returns>Check the console for results</returns>
        /// <response code="200">Connection was requested succesfully</response>
        /// <response code="400">Invalid parameters</response>
        /// <response code="500">Internal error</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Tries to establish a connection to another node. Lasts for 20 seconds",
            Description = "Tries to establish a connection to another node. Sends ping messages and expects pong responses. Use this to test tcp connections between nodes.",
            OperationId = "TestConnectionToPeer"
        )]
        public async Task<ObjectResult> TestConnectionToPeer(string ipAddress, int port)
        {
            try
            {
                if (!IPAddress.TryParse(ipAddress, out var ipAddr)) return BadRequest("Unable to parse the ipAddress");

                var ipEndPoint = new IPEndPoint(ipAddr, port);
                var peer = await _tcpConnectionTester.TestListen(ipEndPoint);
                if (peer != null)
                    return Ok($"Tried to establish connection to peer. Check the console for results.");
                else
                    return Ok($"Unable to connect to peer");

            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<string>(e));
            }
        }

        private DateTime StakeEndTimeCalculationAtMaxPayments(ContractInformationTable contractInfo, TokenLedgerTable sidechainStake)
        {
            var blocksDividedByTotalNumberOfProducers = contractInfo.BlocksBetweenSettlement / (contractInfo.NumberOfFullProducersRequired + contractInfo.NumberOfHistoryProducersRequired + contractInfo.NumberOfValidatorProducersRequired);
            var fullProducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfFullProducersRequired) * contractInfo.MaxPaymentPerBlockFullProducers;
            var historyroducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfHistoryProducersRequired) * contractInfo.MaxPaymentPerBlockHistoryProducers;
            var validatorProducerPaymentPerSettlement = (blocksDividedByTotalNumberOfProducers * contractInfo.NumberOfValidatorProducersRequired) * contractInfo.MaxPaymentPerBlockFullProducers;

            var sidechainStakeString = sidechainStake.Stake.Split(" ")[0];
            var sidechainStakeInUnitsString = sidechainStakeString.Split(".")[0] + sidechainStakeString.Split(".")[1];

            var timesThatRequesterCanPaySettlementWithAllProvidersAtMaxPrice = ulong.Parse(sidechainStakeInUnitsString) / ((fullProducerPaymentPerSettlement + historyroducerPaymentPerSettlement + validatorProducerPaymentPerSettlement));
            return DateTime.UtcNow.AddSeconds((contractInfo.BlockTimeDuration * contractInfo.BlocksBetweenSettlement) * timesThatRequesterCanPaySettlementWithAllProvidersAtMaxPrice);
        }

        private async Task<List<TopProducerEndpointResponse>> ConvertToAndMeasureTopProducerEndpointResponse(List<TopProducerEndpoint> topProducers)
        {
            var topProducersEndpointResponse = new List<TopProducerEndpointResponse>();

            foreach (var producer in topProducers)
            {
                if (!producer.Endpoints.Any()) continue;
                var producerEndpointResponse = new TopProducerEndpointResponse();
                producerEndpointResponse.ProducerInfo = producer.ProducerInfo;
                producerEndpointResponse.Endpoints = new List<EndpointResponse>();
                var requests = new List<HttpWebRequest>();

                foreach (var endpoint in producer.Endpoints)
                {
                    if (!endpoint.Contains("http")) continue;

                    var infoRequest = HttpHelper.ComposeWebRequestGet($"{endpoint}/v1/chain/get_info");
                    requests.Add(infoRequest);
                }

                var requestResults = requests.Select(r => HttpHelper.MeasureWebRequest(r.RequestUri.GetLeftPart(System.UriPartial.Authority), r)).ToList();
                await Task.WhenAll(requestResults);
                var results = requestResults.Select(r => r.Result);

                foreach (var result in results)
                {
                    var endpointResponse = new EndpointResponse();
                    endpointResponse.Endpoint = result.Item1;
                    endpointResponse.ResponseTimeInMs = result.Item2;
                    producerEndpointResponse.Endpoints.Add(endpointResponse);
                }

                producerEndpointResponse.Endpoints = producerEndpointResponse.Endpoints.OrderBy(e => e.ResponseTimeInMs).ToList();
                topProducersEndpointResponse.Add(producerEndpointResponse);
            }

            return topProducersEndpointResponse.ToList();
        }
    }
}
