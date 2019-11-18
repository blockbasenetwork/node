using BlockBase.Domain.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using BlockBase.Utils.Operation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Runtime.SidechainProducer;
using Microsoft.Extensions.Logging;
using BlockBase.DataPersistence;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
using BlockBase.Domain.Eos;
using BlockBase.Utils.Crypto;
using System.Text;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataPersistence.ProducerData;

namespace Blockbase.ProducerD.Commands
{
    class RunProducerCommand : IExecutionCommand
    {
        private const int CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN = 30;
        private const int MAX_NUMBER_OF_TRIES = 5;

        private ILogger _logger;
        private readonly ProducerTestConfigurations _producerTestConfigurations;
        private readonly NetworkConfigurations _networkConfigurations;
        private readonly ICollection<ICollection<string>> _producersData;
        private int _producerNumber;
        private ISidechainProducerService _producerManagementService;
        private SystemConfig SystemConfig { get; set; }
        private IServiceProvider ServiceProvider { get; set; }
        private IMongoDbProducerService _mongoDbProducerService;
        private bool _recover;
        private NodeConfigurations _nodeConfigurations;

        public RunProducerCommand(SystemConfig systemConfig, ProducerTestConfigurations producerTestConfigurations, NetworkConfigurations networkConfigurations, NodeConfigurations nodeConfigurations, ILogger logger, IServiceProvider serviceProvider, IMongoDbProducerService mongoDbProducerService)
        {
            SystemConfig = systemConfig;
            ServiceProvider = serviceProvider;

            _logger = logger;
            _producerTestConfigurations = producerTestConfigurations;
            _networkConfigurations = networkConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _nodeConfigurations = nodeConfigurations;
         

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

            var secret = _nodeConfigurations.SecretPassword;

            if(!_recover){
                var producerConn = new EosStub(CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN, producerPrivateKey, _networkConfigurations.EosNet);
            
                var data = new Dictionary<string, object>()
                {
                    {EosParameterNames.OWNER, producerAccountName},
                    {EosParameterNames.SIDECHAIN, clientAccountName},
                    {EosParameterNames.STAKE, "1200.0000 BBT"}

                };
                
                async Task<OpResult<string>> f1()
                {
                    return await producerConn.SendTransaction(EosMethodNames.ADD_STAKE, _networkConfigurations.BlockBaseTokenContract, producerAccountName, data);
                }
                var transaction = await Repeater.TryAgain(f1, MAX_NUMBER_OF_TRIES);
                _logger.LogInformation("Added producer stake. Tx = " + transaction);

                var secretHash = HashHelper.Sha256Data(Encoding.ASCII.GetBytes(secret));
                var secretHashHash = HashHelper.Sha256Data(secretHash);
                _logger.LogDebug($"Sending candidature with secret hash: {HashHelper.ByteArrayToFormattedHexaString(secretHashHash)} of secret: {HashHelper.ByteArrayToFormattedHexaString(secretHash)}");

                data = new Dictionary<string, object>()
                {   
                    { EosParameterNames.OWNER, clientAccountName},
                    { EosParameterNames.CANDIDATE, producerAccountName},
                    { EosParameterNames.WORK_TIME_IN_SECONDS, 100020 },
                    { EosParameterNames.PUBLIC_KEY, producerPublicKey },
                    { EosParameterNames.SECRET_HASH, HashHelper.ByteArrayToFormattedHexaString(secretHashHash) }
                };

                async Task<OpResult<string>> f2()
                {
                    return await producerConn.SendTransaction(EosMethodNames.ADD_CANDIDATE, _networkConfigurations.BlockBaseOperationsContract, producerAccountName, data);
                }
                transaction = await Repeater.TryAgain(f2, MAX_NUMBER_OF_TRIES);

                _logger.LogInformation("Sent producer application. Tx = " + transaction);
                

                var sidechainPool = new SidechainPool(clientAccountName);

                _producerManagementService.AddSidechainToProducer(sidechainPool);

                try
                {
                    await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(clientAccountName);

                    // IConnector connector = new MySQLConnector("localhost", "root", 3306, "Bb2019");
                    // var dbServerManager = new DbServerManager("localhost", connector);
                }
                
                catch(Exception ex)
                {
                    _logger.LogDebug("Error adding sidechain to recoverDB: " + ex.Message);
                }               
            }
            else await _producerManagementService.GetSidechainsFromRecoverDB();
        }

        public string GetCommandHelp()
        {
            return "rm <producerNumber> n/r";
        }

        public bool TryParseCommand(string commandStr)
        {
            try
            {
                var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (commandData.Length != 3) return false;

                if (commandData[0] != "rm") return false;
                if (commandData[2] != "n" && commandData[2] != "r") return false;
                if (!int.TryParse(commandData[1], out var producerNumber)) return false;
                if (producerNumber < 1 || producerNumber > 3) return false;
                _producerNumber = producerNumber;
                _producerManagementService = ServiceProvider.GetService<ISidechainProducerService>();

                _recover = commandData[2] == "r";

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
