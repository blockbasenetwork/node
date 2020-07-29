

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
using BlockBase.Utils;
using Newtonsoft.Json;
using BlockBase.Domain.Results;
using BlockBase.Domain.Enums;
using BlockBase.Runtime.Network;
using BlockBase.Node.Filters;
using BlockBase.Domain.Endpoints;
using BlockBase.Node.Commands.Network;

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
        private readonly PeerConnectionsHandler _peerConnectionsHandler;
        private readonly TcpConnectionTester _tcpConnectionTester;

        public NetworkController(ILogger<NetworkController> logger, IMainchainService mainchainService, TcpConnectionTester tcpConnectionTester, PeerConnectionsHandler peerConnectionsHandler)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _tcpConnectionTester = tcpConnectionTester;
            _peerConnectionsHandler = peerConnectionsHandler;
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
            var command = new GetSidechainConfigurationCommand(_logger, _mainchainService, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetProviderCandidatureStateCommand(_logger, _mainchainService, sidechainName, accountName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetSidechainStateCommand(_logger, _mainchainService, sidechainName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetAccountStakeRecordsCommand(_logger, _mainchainService, accountName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetTopProducersEndpointsCommand(_logger);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetAllBlockBaseSidechainsCommand(_logger, network);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new GetCurrentUnclaimedRewardsCommand(_logger, _mainchainService, accountName);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
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
            var command = new TestConnectionToPeerCommand(_logger, _mainchainService, _tcpConnectionTester, ipAddress, port);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
        }

        /// <summary>
        /// GGets the list of peers this node knows and the connection status
        /// </summary>
        /// <returns>The list of peers</returns>
        /// <response code="200">List of peers returned successfully</response>
        /// <response code="404">Connected peers not found</response>
        /// <response code="500">Internal error</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Gets the list of peers this node knows and the connection status",
            Description = "Checks the node connections and returns a list of peers this node currently has knowledge of and the connectino state with each of them",
            OperationId = "GetPeerConnectionsState"
        )]
        public async Task<ObjectResult> GetPeerConnectionsState()
        {
            var command = new GetPeerConnectionStateCommand(_logger, _peerConnectionsHandler);
            var result = await command.Execute();

            return StatusCode((int)result.HttpStatusCode, result.OperationResponse);
        }

        [HttpPost]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ObjectResult ManualDisconnect(string ipAddress, int port)
        {
            if (!IPAddress.TryParse(ipAddress, out var ipAddr)) return BadRequest(new OperationResponse(false, "Unable to parse the ipAddress"));
            var peerConnection = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Where(c => c.IPEndPoint.Address == ipAddr && c.IPEndPoint.Port == port).SingleOrDefault();

            if (peerConnection == null) return BadRequest(new OperationResponse(false, "Not connected to this peer"));
            _peerConnectionsHandler.Disconnect(peerConnection);

            return Ok(new OperationResponse(true, "Disconnected from peer"));
        }
    }
}
