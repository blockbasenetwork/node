using System;
using System.Collections.Generic;
using BlockBase.Domain.Configurations;
using BlockBase.DataPersistence;
using BlockBase.Runtime.SidechainProducer;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using BlockBase.Network.Mainchain;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;

namespace Blockbase.ProducerD.Commands
{
    public class RecoverCommand : IExecutionCommand
    {
        private const int CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN = 30;
        private ILogger _logger;
        private readonly ProducerTestConfigurations _producerTestConfigurations;
        private readonly NetworkConfigurations _networkConfigurations;
        private readonly ICollection<ICollection<string>> _producersData;
        private int _producerNumber;

        private ISidechainProducerService _producerManagementService;
        private SystemConfig SystemConfig { get; set; }
        private IServiceProvider ServiceProvider { get; set; }
        private IMongoDbProducerService _mongoDbProducerService;

        public RecoverCommand(SystemConfig systemConfig, ProducerTestConfigurations producerTestConfigurations, NetworkConfigurations networkConfigurations, ILogger logger, IServiceProvider serviceProvider, IMongoDbProducerService mongoDbProducerService)
        {

            SystemConfig = systemConfig;
            ServiceProvider = serviceProvider;

            _logger = logger;
            _producerTestConfigurations = producerTestConfigurations;
            _networkConfigurations = networkConfigurations;
            _mongoDbProducerService = mongoDbProducerService;

            var producer1Data = new ReadOnlyCollection<string>
                (new List<string> {
                    _producerTestConfigurations?.Producer1Name,
                    _producerTestConfigurations?.Producer1PrivateKey,
                    _producerTestConfigurations?.Producer1PublicKey
                });

            var producer2Data = new ReadOnlyCollection<string>
                (new List<string> {
                    _producerTestConfigurations?.Producer2Name,
                    _producerTestConfigurations?.Producer2PrivateKey,
                    _producerTestConfigurations?.Producer2PublicKey
                });

            var producer3Data = new ReadOnlyCollection<string>
                (new List<string> {
                    _producerTestConfigurations?.Producer3Name,
                    _producerTestConfigurations?.Producer3PrivateKey,
                    _producerTestConfigurations?.Producer3PublicKey
                });

            _producersData = new ReadOnlyCollection<ICollection<string>>(new List<ICollection<string>> {producer1Data, producer2Data, producer3Data});

        }



        public async Task ExecuteAsync()
        {
            var producerAccountName = _producersData.ElementAt(_producerNumber - 1).ElementAt(0);
            var producerPrivateKey = _producersData.ElementAt(_producerNumber - 1).ElementAt(1);
            var producerPublicKey = _producersData.ElementAt(_producerNumber - 1).ElementAt(2);
            var clientAccountName = _producerTestConfigurations?.ClientAccountName;

            var producerConn = new EosStub(CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN, producerPrivateKey, _networkConfigurations.EosNet);

            await _producerManagementService.GetSidechainsFromRecoverDB();
            
        }

        public string GetCommandHelp()
        {
            return "recover";
        }

        public bool TryParseCommand(string commandStr)
        {
            try
            {
                var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (commandData.Length != 2) return false;

                if (commandData[0] != "recover") return false;
                if (!int.TryParse(commandData[1], out var producerNumber)) return false;
                if (producerNumber < 1 || producerNumber > 3) return false;
                _producerNumber = producerNumber;
                _producerManagementService = ServiceProvider.GetService<ISidechainProducerService>();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to run producer. Exception thrown {ex.Message}");
                return false; 
            }
        }
    }

}