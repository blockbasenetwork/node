using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using Microsoft.Extensions.Logging;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.SidechainProducer;



namespace Blockbase.ProducerD.Commands
{
    public class ContractManagerCommand : IExecutionCommand
    {
        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }
        private INetworkService NetworkService { get; set; }
        private IServiceProvider _serviceProvider { get; set; }
         private const uint TRANSACTION_EXPIRATION_TIME_IN_SECONDS = 30;    
        private SystemConfig _systemConfig;
        private ILogger _logger;
        private EosStub _clientConnection;
        private NetworkConfigurations _networkConfigurations;
        private ISidechainProducerService _producer1ManagementService;

        public ContractManagerCommand(SystemConfig config,NetworkConfigurations networkConfigurations, IServiceProvider serviveProvider)
        {
            _systemConfig = config;
            _serviceProvider = serviveProvider;
            _networkConfigurations = networkConfigurations;
        }

        public async Task ExecuteAsync()
        {
            // _producer1ManagementService = _serviceProvider.GetService<ISidechainProducerService>();
            // var sidechainPool = new SidechainPool(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            // var a = _producer1ManagementService.ClientThreadCheck(sidechainPool);
         

            // Console.WriteLine("Client deferred transaction thread started...");
        }

        public string GetCommandHelp()
        {
            return "contmc <ip> <port>";
        }

        public bool TryParseCommand(string commandStr)
        {
            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandData.Length != 3) return false;
            if (commandData[0] != "contmc") return false;

            if (!IPAddress.TryParse(commandData[1], out var ipAddress)) return false;
            if (!int.TryParse(commandData[2], out var port)) return false;

            IPAddress = ipAddress;
            Port = port;
            
            return true;
        }
    }
}