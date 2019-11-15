using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using BlockBase.Network.Mainchain;
using BlockBase.Network.History;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.SidechainProducer;
using System.Text;
using BlockBase.Utils.Crypto;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Network.Mainchain.Pocos;
using Swashbuckle.AspNetCore.Annotations;

using BlockBase.Network.History.Pocos;

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
        private readonly IHistoryService _historyService;
        private IMongoDbProducerService _mongoDbProducerService;

        public ProducerController(ILogger<ChainController> logger, IOptions<NodeConfigurations> nodeConfigurations, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, IHistoryService historyService)
        {
            NodeConfigurations = nodeConfigurations?.Value;

            _logger = logger;
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _historyService = historyService;

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
        //TODO nao sei ainda

        [HttpGet]
        public async Task<ObjectResult> GetProducerCandidature(string chainName, bool forceDelete = false)
        {
            try
            {
                var contractStates = await _mainchainService.RetrieveContractState(chainName);
                var candidatureTable = await _mainchainService.RetrieveCandidates(chainName);

                if (!contractStates.CandidatureTime) return BadRequest(new OperationResponse<bool>(false, "Sidechain not in candidature time!"));

                return Ok(new OperationResponse<bool>(_mainchainService.IsCandidateInTable(candidatureTable)));
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

        //TODO: Change all returns, create objects for response types

        /// <summary>
        /// Returns the list of all producers on BlockBase
        /// </summary>
        /// <returns>The list of producers</returns>
        /// <response code="200">Returns the list of producers</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets a list of all current producers",
            Description = "Requires login to access",
            OperationId = "GetProducerList"
        )]
        public async Task<ObjectResult> GetProducerList()
        {
            try
            {
                //var producerList = await _historyService.GetProducerList();
                List<Producers> producerList = new List<Producers>();
                for (int i = 0; i < 10; i++)
                {
                    Producers producer = new Producers();
                    producer.Name = "producer" + i;
                    producer.ActiveSidechains = Convert.ToUInt32(i + 3);
                    producer.TotalSidechains = Convert.ToUInt32(i + 5);
                    producer.TotalReward = (i + 24) + " BBT";
                    producer.MemberSince = new DateTime();
                    producer.NumberOfBlackLists = Convert.ToUInt32(i + 1);
                    producer.LastActivity = new DateTime();
                    producerList.Add(producer);
                }
                return Ok(new OperationResponse<List<Producers>>(producerList, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<SidechainDetail>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<SidechainDetail>(e, "Some error occorued."));
            }
        }

        /// <summary>
        /// Returns the list of all the sidechains on BlockBase
        /// </summary>
        /// <returns>The list of sidechains</returns>
        /// <response code="200">Returns the list of sidechains</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets a list of all sidechain and it's current state",
            Description = "Requires login to access",
            OperationId = "GetSidechainList"
        )]
        public async Task<ObjectResult> GetSidechainList()
        {
            try
            {
                //var sidechainList = await _historyService.GetSidechainList();
                List<Sidechains> sidechainList = new List<Sidechains>();
                for (int i = 0; i < 10; i++)
                {
                    Sidechains sidechain = new Sidechains();
                    sidechain.Name = "teste" + i;
                    if (i % 2 == 0)
                    {
                        sidechain.State = "Producing";
                        sidechain.IsProductionTime = true;
                    }
                    else
                    {
                        sidechain.State = "Candidature";
                        sidechain.IsProductionTime = false;
                    }
                    sidechain.ActualProducers = 21;
                    sidechain.NeededProducers = 21;
                    sidechain.Reward = 10 + i + " BBT";
                    sidechain.BlockHeader = "asawq" + i;
                    sidechain.TotalBlocks = Convert.ToUInt32(i * 15 + 100);
                    sidechainList.Add(sidechain);
                }
                return Ok(new OperationResponse<List<Sidechains>>(sidechainList, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<SidechainDetail>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<SidechainDetail>(e, "Some error occorued."));
            }
        }

        /// <summary>
        /// Returns all the details of a determined producer
        /// </summary>
        /// <param name="producerName">Account name of a specific producer</param>
        /// <returns>The list of details about a producer</returns>
        /// <response code="200">Candidature sent with success!</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets a list of all the details of a specific producer",
            Description = "Requires login to access",
            OperationId = "GetProducerDetails"
        )]
        public async Task<ObjectResult> GetProducerDetail(string producerName)
        {
            try
            {
                //var producerDetails = await _historyService.GetProducerDetail(producerName);
                List<ProducerDetail> producerDetails = new List<ProducerDetail>();
                ProducerDetail producer = new ProducerDetail();

                producer.WorkTime = new DateTime();
                if (producerName != "teste")
                {
                    producer.IsSidechainInProduction = true;
                    producer.IsRewardAvailable = true;
                    producer.SidechainState = "producing";
                    producer.StakeCommited = 121412 + " BBT";
                    producer.ProducerStateInChain = "Producing";
                    producer.SidechainName = "teste1";
                }
                else
                {
                    producer.IsSidechainInProduction = false;
                    producer.IsRewardAvailable = false;
                    producer.SidechainState = "Candidate";
                    producer.StakeCommited = 231 + " BBT";
                    producer.ProducerStateInChain = "Candidature";
                    producer.SidechainName = "teste112";
                }
                producerDetails.Add(producer);
                return Ok(new OperationResponse<List<ProducerDetail>>(producerDetails, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<List<ProducerDetail>>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<List<ProducerDetail>>(e, "Some error occorued."));
            }
        }

        /// <summary>
        /// Returns all the details of a determined sidechain
        /// </summary>
        /// <param name="chainName">Account name of the Sidechain</param>
        /// <returns>The list of details about a sidechain</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets a list of all the details of a specific sidechain",
            Description = "Requires login to access",
            OperationId = "GetSidechainDetails"
        )]
        public async Task<Object> GetSidechainDetail(string chainName)
        {
            try
            {
                //var sidechainDetail = await _historyService.GetSidechainDetail(chainName);
                SidechainDetail sidechainDetail = new SidechainDetail();
                sidechainDetail.State = "Producing";
                sidechainDetail.IsProducting = true;
                sidechainDetail.MinCandidateStake = 1241 + " BBT";
                sidechainDetail.BlockThreshold = 15;
                sidechainDetail.CurrentBlockHeaderHash = "adqwqa121f1wfsa";
                sidechainDetail.Reward = 12 + " BBT";
                sidechainDetail.NeededProducerNumber = 15;
                sidechainDetail.ActualProducerNumber = 14;
                sidechainDetail.TotalStake = 1351251 + " BBT";
                sidechainDetail.TotalBlocks = 1242;
                sidechainDetail.CurrentProducer = "teste1";
                return Ok(new OperationResponse<SidechainDetail>(sidechainDetail, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<SidechainDetail>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<SidechainDetail>(e, "Some error occorued."));
            }
        }
        /// <summary>
        /// Returns the list of all the sidechains on BlockBase
        /// </summary>
        /// <returns>The list of sidechains</returns>
        /// <response code="200">Returns the list of sidechains</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets a list of all sidechain and it's current state",
            Description = "Requires login to access",
            OperationId = "GetSidechainList"
        )]
        public async Task<ObjectResult> GetSidechainBlockHeaders(string chainName)
        {
            try
            {
                //var sidechainDetail = await _historyService.GetSidechainDetail(chainName);
                List<SidechainBlockHeader> blockList = new List<SidechainBlockHeader>();
                for (int i = 0; i < 5; i++)
                {
                    SidechainBlockHeader blockheader = new SidechainBlockHeader();
                    blockheader.BlockHash = "asjdhqbd121e";
                    blockheader.PreviousBlockHash = "asfhw12hd1";
                    blockheader.TransactionsNumber = 141;
                    blockheader.BlockNumber = Convert.ToUInt32(12 + i);
                    blockheader.Producer = "teste" + i;
                    blockheader.CreationDate = new DateTime();
                    blockList.Add(blockheader);
                }
                return Ok(new OperationResponse<List<SidechainBlockHeader>>(blockList, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<SidechainDetail>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<SidechainDetail>(e, "Some error occorued."));
            }
        }

        [HttpGet]
        public async Task<ObjectResult> GetSidechainBlackList(string chainName)
        {
            try
            {
                //var sidechainDetail = await _historyService.GetSidechainDetail(chainName);
                List<SidechainBlackList> blackList = new List<SidechainBlackList>();
                for (int i = 0; i < 10; i++)
                {
                    SidechainBlackList blacklisted = new SidechainBlackList();
                    blacklisted.ProducerName = "teste" + i;
                    blacklisted.StakeLost = 12241 + " BTT";
                    blacklisted.Date = new DateTime();
                    blacklisted.BlockHeader = "1hbasfj3" + i;
                    blacklisted.BlockNumber = Convert.ToUInt32((12 * 5) * i);
                    blackList.Add(blacklisted);
                }
                return Ok(new OperationResponse<List<SidechainBlackList>>(blackList, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<SidechainDetail>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<SidechainDetail>(e, "Some error occorued."));
            }
        }

        [HttpGet]

        public async Task<Object> GetSidechainProducer(string producerName)
        {
            try
            {
                //var sidechainDetail = await _historyService.GetSidechainDetail(chainName);
                List<SidechainProducer> producerList = new List<SidechainProducer>();
                for (int i = 0; i < 10; i++)
                {
                    SidechainProducer producer = new SidechainProducer();
                    producer.ProducerName = "teste" + i;
                    producer.Date = new DateTime();
                    producer.WorkTime = 12312312 + i;
                    producer.StakeCommited = Convert.ToUInt32(1241 + (i * 5)) + " BBT";
                    producer.Warning = "Warning";
                    producer.BlocksProduced = Convert.ToUInt32((12 * 3) * i);
                    producerList.Add(producer);
                }
                return Ok(new OperationResponse<List<SidechainProducer>>(producerList, "Sucessfull"));
            }
            catch (WebException eosConnection)
            {
                return NotFound(new OperationResponse<SidechainDetail>(eosConnection, "Can't connect with EOS net."));
            }
            catch (Exception e)
            {
                return BadRequest(new OperationResponse<SidechainDetail>(e, "Some error occorued."));
            }
        }


    }
}
