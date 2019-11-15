using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BlockBase.Utils;
using System.Net;
using BlockBase.Domain.Configurations;
using BlockBase.Runtime.Network;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Controllers
{
    //TODO: Remove controller, no longer necessary but still useful for quick testing of api
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class NetworkController : ControllerBase
    {
        private SystemConfig SystemConfig { get; set; }
        private NetworkConfigurations NetworkConfigurations { get; set; }
        private INetworkService NetworkService { get; set; }
        private readonly ILogger _logger;

        public NetworkController(SystemConfig systemConfig, IOptions<NetworkConfigurations> networkConfigurations, INetworkService networkService, ILogger<NetworkController> logger)
        {
            SystemConfig = systemConfig;
            NetworkConfigurations = networkConfigurations?.Value;
            NetworkService = networkService;
            
            _logger = logger;
        }

        [HttpPost]
        public void ConfigureTestNetwork(string ipAddress, string tcpPort, string udpPort)
        {
            SystemConfig.IPAddress = IPAddress.TryParse(ipAddress, out var parsedIpAddress) ? parsedIpAddress : null;
            SystemConfig.TcpPort = int.TryParse(tcpPort, out var parsedTcpPort) ? parsedTcpPort : -1;
            SystemConfig.UdpPort = int.TryParse(udpPort, out var parsedUdpPort) ? parsedUdpPort : -1;
        }

        [HttpPost]
        public async Task ConnectToPeer(string ipAddress, int tcpPort)
        {
            try
            {
                var addressParsed = IPAddress.TryParse(ipAddress, out var parsedIpAddress);
                if (addressParsed) await NetworkService.ConnectAsync(new IPEndPoint(parsedIpAddress, tcpPort));
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception thrown trying to connect to peer: {e.Message} \n{e.StackTrace}");
            }
        }
    }
}
