using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.Sidechain;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using BlockBase.Utils.Operation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    internal class TestContractConnectionCommand : IExecutionCommand
    {
        private const int CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN = 30;
        private const int CANDIDATURE_TIME_IN_SECONDS = 15;
        private const int BLOCKS_BETWEEN_SETTLEMENT = 20;
        private const int SEND_TIME_IN_SECONDS = 10;
        private const int RECEIVE_TIME_IN_SECONDS = 10;
        private const int MAX_NUMBER_OF_TRIES = 5;

        private int _producersNumber;

        private ILogger _logger;
        private readonly ProducerTestConfigurations _producerTestConfigurations;
        private readonly NetworkConfigurations _networkConfigurations;

        private SystemConfig SystemConfig { get; set; }
        private IServiceProvider ServiceProvider { get; set; }

        public TestContractConnectionCommand(SystemConfig systemConfig, ProducerTestConfigurations producerTestConfigurations, ILogger logger, NetworkConfigurations networkConfigurations, IServiceProvider serviceProvider)
        {
            SystemConfig = systemConfig;
            ServiceProvider = serviceProvider;
            _logger = logger;
            _producerTestConfigurations = producerTestConfigurations;
            _networkConfigurations = networkConfigurations;
        }

        public async Task ExecuteAsync()
        {
            var clientAccountName = _producerTestConfigurations?.ClientAccountName;
            var clientPrivateKey = _producerTestConfigurations?.ClientAccountPrivateKey;
            var clientPublicKey = _producerTestConfigurations?.ClientAccountPublicKey;
            var clientConn = new EosStub(CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN, clientPrivateKey, _networkConfigurations.EosNet);

            string transaction;

            async Task<OpResult<string>> f1()
            {
                return await clientConn.SendTransaction(EosMethodNames.START_CHAIN, _networkConfigurations.BlockBaseOperationsContract, clientAccountName, TestConstantVariables.DEFAULT_START_CHAIN_DATA);
            }

            transaction =  await Repeater.TryAgain(f1, MAX_NUMBER_OF_TRIES);

            _logger.LogInformation("Sent startchain. Tx = " + transaction);

            var data = new Dictionary<string, object>()
            {
                {EosParameterNames.OWNER, clientAccountName},
                {EosParameterNames.SIDECHAIN, clientAccountName},
                {EosParameterNames.STAKE, "100000.0000 BBT"}
            };

            async Task<OpResult<string>> f2()
            {
                return await clientConn.SendTransaction(EosMethodNames.ADD_STAKE, _networkConfigurations.BlockBaseTokenContract, clientAccountName, data);
            }
            transaction = await Repeater.TryAgain(f2, MAX_NUMBER_OF_TRIES);
            _logger.LogInformation("Added client stake. Tx = " + transaction);


            data = TestConstantVariables.DEFAULT_CONFIG_CHAIN_DATA;
            var infojson = (Dictionary<string,object>)data[EosParameterNames.CONFIG_INFO_JSON];

            infojson[EosParameterNames.PRODUCERS_NUMBER] = _producersNumber;
            infojson[EosParameterNames.KEY] = _producerTestConfigurations.ClientAccountName;

            async Task<OpResult<string>> f3()
            {
                return await clientConn.SendTransaction(EosMethodNames.CONFIG_CHAIN, _networkConfigurations.BlockBaseOperationsContract, clientAccountName, data);
            }
            transaction = await Repeater.TryAgain(f3, MAX_NUMBER_OF_TRIES);

            _logger.LogInformation("Sent init config. Tx = " + transaction);

            
        }

        public string GetCommandHelp()
        {
            return "cse";
        }

        public bool TryParseCommand(string commandStr)
        {

            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandData.Length != 2) return false;
            if (commandData[0] != "cse") return false;

            if (!int.TryParse(commandData[1], out var producersNumber)) return false;
            if (producersNumber < 1 || producersNumber > 3) return false;

            _producersNumber = producersNumber;
            return true;
        }
    }
}