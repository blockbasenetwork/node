using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    public class ConfigureNetworkServiceCommand : IConfigurationCommand
    {
        private ILogger _logger;
        public SystemConfig SystemConfig { get; set; }
        public readonly INetworkService _networkService;

        public ConfigureNetworkServiceCommand(SystemConfig config, ILogger logger, INetworkService networkService)
        {
            SystemConfig = config;
            _logger = logger;
            _networkService = networkService;
        }

        public async Task ExecuteAsync()
        {
            _networkService.Run();
        }

        public string GetCommandHelp()
        {
            return "cfg ntw <ip> <tcp port> <udp port>";
        }

        public bool TryParseCommand(string commandStr)
        {
            try
            {
                var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (commandData.Length != 5) return false;

                if (commandData[0] != "cfg" || commandData[1] != "ntw") return false;

                if (!IPAddress.TryParse(commandData[2], out var ipAddress)) return false;
                SystemConfig.IPAddress = ipAddress;

                if (!int.TryParse(commandData[3], out var tcpPort)) return false;
                SystemConfig.TcpPort = tcpPort;

                if (!int.TryParse(commandData[4], out var udpPort)) return false;
                SystemConfig.UdpPort = udpPort;

                _logger.LogInformation("Configured Network: " + ipAddress + " tcp: " + tcpPort + " udp: " + udpPort);

                return true;
            }
            catch { return false; }
        }
    }
}