using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain;
using BlockBase.Runtime.SidechainProducer;
using Blockbase.ProducerD.Commands.Interfaces;
using Blockbase.Tests;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Xunit;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.Network;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using System.Linq;
using BlockBase.Network.Sidechain;
using BlockBase.Domain.Enums;
using System;
using System.Collections.Generic;
using BlockBase.Network.Connectors;
using BlockBase.DataPersistence;
using BlockBase.DataPersistence.ProducerData;

namespace Blockbase.ProducerD.Commands
{
    class AllTestCommand : IExecutionCommand
    {
        private const uint TRANSACTION_EXPIRATION_TIME_IN_SECONDS = 30;    
        private SystemConfig _systemConfig;
        private ILogger _logger;
        private SystemConfig _systemConfig1;
        private SystemConfig _systemConfig2;
        private SystemConfig _systemConfig3;
        private IOptions<NetworkConfigurations> _networkConfigurations;
        private IOptions<ProducerTestConfigurations> _producerTestConfigurations;
        private EosStub _clientConnection;
        private EosStub _producer1Connection;
        private EosStub _producer2Connection;
        private EosStub _producer3Connection;
        private IMongoDbProducerService _mongoDbProducerService;
        private StartChainTest _startChainTest;
        private ConfigChainTest _configChainTest;
        private AddCandidateTest _addCandidateTest;
        private ISidechainProducerService _producer1ManagementService;
        private ISidechainProducerService _producer2ManagementService;
        private ISidechainProducerService _producer3ManagementService;
        private const int TCP_PORT_1 = 40401;
        private const int TCP_PORT_2 = 40402;
        private const int TCP_PORT_3 = 40403;
        private IServiceProvider ServiceProvider { get; set; }

        public AllTestCommand(ILogger logger, SystemConfig systemConfig, IOptions<NetworkConfigurations> networkConfigurations,  IOptions<ProducerTestConfigurations> producerTestConfigurations, IServiceProvider serviceProvider, IMongoDbProducerService mongoDbProducerService)
        {
            _logger = logger;
            _systemConfig = systemConfig;
            _networkConfigurations = networkConfigurations;
            _producerTestConfigurations = producerTestConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            ServiceProvider = serviceProvider;
        }

        public async Task ExecuteAsync()
        {
            var testComplete = new List<TestStateEnum>(){
                TestStateEnum.ResetChain, TestStateEnum.StartChainWithWrongValues, TestStateEnum.StartChain, 
                TestStateEnum.ConfigChainWithWrongValues, TestStateEnum.ConfigChain, TestStateEnum.Add3Candidates, 
                TestStateEnum.AwaitForCandidatureTimeToEnd, TestStateEnum.CheckIf2StartedSendTimeAnd1DidNot};

            var test = new List<TestStateEnum>(){
                TestStateEnum.ResetChain, TestStateEnum.StartChain, TestStateEnum.ConfigChain, TestStateEnum.Add3Candidates, 
                TestStateEnum.AwaitForCandidatureTimeToEnd, TestStateEnum.CheckIf2StartedSendTimeAnd1DidNot};
                
            foreach(TestStateEnum state in test ){
                switch(state)
                {
                    case TestStateEnum.ResetChain:
                        await CommonMethods.ResetSmartContract(_clientConnection);
                        break;
                    
                    case TestStateEnum.StartChainWithWrongValues:
                        await _startChainTest.TryToStartChainWithInvalidParameters();
                        break;

                    case TestStateEnum.StartChain:
                        await _startChainTest.TryToStartChainWithDefaultValues();
                        break;
                    
                    case TestStateEnum.ConfigChainWithWrongValues:
                        await _configChainTest.TryToConfigureChainWithInvalidParameters();
                        break;
                    
                    case TestStateEnum.ConfigChain:
                        await _configChainTest.TryToConfigureChainWithDefaultValues();
                        break;

                    case TestStateEnum.CheckIfConfigueTimeStarted:
                        await CheckIfConfigureTimeStarted();
                        break;

                    case TestStateEnum.AddCandidateWithWrongValues:
                        await TryToAddCandidateWithInvalidParameters();
                        break;
                    
                    case TestStateEnum.AwaitForCandidatureTimeToEnd:
                        await Task.Delay((int)(TestConstantVariables.DEFAULT_CANDIDATURE_TIME + 1) * 1000);
                        break;
                    
                    case TestStateEnum.Add3Candidates:
                        await TestAdding3Producers();
                        break;

                    case TestStateEnum.CheckIf2StartedSendTimeAnd1DidNot:
                        CheckIf2StartedSendTimeAnd1DidNot();
                        break;
                    
                    case TestStateEnum.Add1Candidate:
                        await TestWithNotEnoughCandidates();
                        break;

                    

                }
            }
        }

        private async Task TestAdding3Producers()
        {
            await TestIfProducersAreAdded(true);

            SetKeysAndEndPointAndAddSidechainPoolToProducers();
        }

        private void CheckIf2StartedSendTimeAnd1DidNot()
        {
            // var state1 = _producer1ManagementService.SidechainServices[TestConstantVariables.CLIENT_ACCOUNT_NAME].Sidechain.State;
            // var state2 = _producer2ManagementService.SidechainServices[TestConstantVariables.CLIENT_ACCOUNT_NAME].Sidechain.State;
            // var state3 = _producer3ManagementService.SidechainServices[TestConstantVariables.CLIENT_ACCOUNT_NAME].Sidechain.State;

            // List<SidechainPoolStateEnum> listStates = new List<SidechainPoolStateEnum>() { state1, state2, state3};
            // var countSendState = 0;
            // var countCheckSmartContract = 0;

            // foreach(SidechainPoolStateEnum state in listStates)
            // {
            //     if(state == SidechainPoolStateEnum.ProducerSendIP || state == SidechainPoolStateEnum.InitProducerSendIP) countSendState++;
            //     // else if(state == SidechainPoolStateEnum.CheckSmartContract || state == SidechainPoolStateEnum.WaitingForSettlement) countCheckSmartContract++;
            // }
            
            // Assert.False(countSendState != 2 || countCheckSmartContract != 1, "A producer is in the wrong state.");
        }
        private async Task TestWithNotEnoughCandidates()
        {
            await TestIfProducersAreAdded(false);
        }

        private async Task<MethodInfo[]> TryToAddCandidateWithInvalidParameters()
        {
            MethodInfo[] methods = _addCandidateTest.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods) await (Task)method.Invoke(_addCandidateTest, new object[0]);
            return methods;
        }

        private void SetKeysAndEndPointAndAddSidechainPoolToProducers()
        {
            var sidechainPool1 = new SidechainPool(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            var sidechainPool2 = new SidechainPool(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            var sidechainPool3 = new SidechainPool(TestConstantVariables.CLIENT_ACCOUNT_NAME);

            _producer1ManagementService.AddSidechainToProducer(sidechainPool1);
            _producer2ManagementService.AddSidechainToProducer(sidechainPool2);
            _producer3ManagementService.AddSidechainToProducer(sidechainPool3);
        }

        private async Task TestIfProducersAreAdded( bool enoughProducers)
        {
            await TestIfCandidateIsAdded(_producer1Connection, TestConstantVariables.PRODUCER1_ACCOUNT_NAME, TestConstantVariables.PRODUCER1_PUBLIC_KEY);

            if(enoughProducers){
                await TestIfCandidateIsAdded(_producer2Connection, TestConstantVariables.PRODUCER2_ACCOUNT_NAME, TestConstantVariables.PRODUCER2_PUBLIC_KEY);

                await TestIfCandidateIsAdded(_producer3Connection, TestConstantVariables.PRODUCER3_ACCOUNT_NAME, TestConstantVariables.PRODUCER3_PUBLIC_KEY);
            }
        }

        

        public async Task CheckIfConfigureTimeStarted()
        {
            // var contractState= await _clientConnection.RetrieveContractState(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            // Assert.False(!contractState.ConfigTime, "Configure time didn't start on chain.");
            // _logger.LogDebug("Test passed: Configure time started in chain.");
        }

        public async Task TestIfCandidatureTimeStarted()
        {
            // var contractState= await _clientConnection.RetrieveContractState(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            // Assert.False(!contractState.CandidatureTime, "Candidature time didn't start on chain.");
            // _logger.LogDebug("Test passed: Candidature time started in chain.");
        }

        public async Task TestIfSendTimeStarted(ISidechainProducerService producerManagementService)
        {
            // var contractState= await _clientConnection.RetrieveContractState(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            // Assert.False(!contractState.IPSendTime, "IP send time didn't start on chain.");
            // _logger.LogDebug("Test passed: IP send time started in chain.");
            // var state = producerManagementService.SidechainServices[TestConstantVariables.CLIENT_ACCOUNT_NAME].Sidechain.State;
            // Assert.False(state != SidechainPoolStateEnum.ProducerSendIP && state != SidechainPoolStateEnum.InitProducerSendIP, "IP send time didn't start on producer.");
            // _logger.LogDebug("Test passed: IP send time started in producer.");     
        }

         public async Task TestIfReceiveTimeStarted(ISidechainProducerService producerManagementService)
        {
            // var contractState= await _clientConnection.RetrieveContractState(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            // Assert.False(!contractState.IPReceiveTime, "IP receive time didn't start on chain.");
            // _logger.LogDebug("Test passed: IP receive time started in chain.");
            // var state = producerManagementService.SidechainServices[TestConstantVariables.CLIENT_ACCOUNT_NAME].Sidechain.State;
            // Assert.False(state != SidechainPoolStateEnum.ProducerReceiveIPs, "IP receive time didn't start on producer.");
            // _logger.LogDebug("Test passed: IP receive time started in producer.");     
        }

        

        public async Task TestIfCandidateIsAdded(EosStub producerConnection, string producerAccountName, string producerPublicKey)
        {
            // await CommonMethods.AddCandidate(producerConnection, producerAccountName, producerPublicKey);
            // var candidatesTable = await producerConnection.RetrieveCandidates(TestConstantVariables.CLIENT_ACCOUNT_NAME);
            // var added = candidatesTable.Exists(c => c.Key == producerAccountName);  
            // Assert.False(!added, "Candidate wasn't added.");
            // _logger.LogDebug($"Test passed: Candidate {producerAccountName} added.");
        }

        public string GetCommandHelp()
        {
            return "test";
        }

        public bool TryParseCommand(string commandStr)
        {
            if(commandStr == "test"){

                _clientConnection = new EosStub(TRANSACTION_EXPIRATION_TIME_IN_SECONDS, TestConstantVariables.CLIENT_PRIVATE_KEY, TestConstantVariables.LOCAL_NET);
                _producer1Connection = new EosStub(TRANSACTION_EXPIRATION_TIME_IN_SECONDS, TestConstantVariables.PRODUCER1_PRIVATE_KEY, TestConstantVariables.LOCAL_NET);
                _producer2Connection = new EosStub(TRANSACTION_EXPIRATION_TIME_IN_SECONDS, TestConstantVariables.PRODUCER2_PRIVATE_KEY, TestConstantVariables.LOCAL_NET);
                _producer3Connection = new EosStub(TRANSACTION_EXPIRATION_TIME_IN_SECONDS, TestConstantVariables.PRODUCER3_PRIVATE_KEY, TestConstantVariables.LOCAL_NET);
                
                _startChainTest = new StartChainTest(_logger, _clientConnection, _producer1Connection);
                _configChainTest = new ConfigChainTest(_logger, _clientConnection, _producer1Connection);
                _addCandidateTest = new AddCandidateTest(_logger, _clientConnection, _producer1Connection);

                var ipAddress = IPAddress.Parse("127.0.0.1");

                _systemConfig1 = new SystemConfig(ipAddress, TCP_PORT_1);
                _systemConfig2 = new SystemConfig(ipAddress, TCP_PORT_2);
                _systemConfig3 = new SystemConfig(ipAddress, TCP_PORT_3);

                var sidechainKeeper1 = new SidechainKeeper();
                var sidechainKeeper2 = new SidechainKeeper();
                var sidechainKeeper3 = new SidechainKeeper();

                var networkService = ServiceProvider.GetService<INetworkService>();

                var networkService1 = new NetworkService(ServiceProvider, _systemConfig1);
                var networkService2 = new NetworkService(ServiceProvider, _systemConfig2);
                var networkService3 = new NetworkService(ServiceProvider, _systemConfig3);               

                var loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
                var loggerP = loggerFactory.CreateLogger<PeerConnectionsHandler>();
                var loggerM = loggerFactory.CreateLogger<SidechainProducerService>();
                var loggerT = loggerFactory.CreateLogger<TcpConnector>();

                // _producer1ManagementService = new SidechainProducerService(sidechainKeeper1, 
                // new PeerConnectionsHandler(networkService1, sidechainKeeper1, _systemConfig1, loggerP), 
                // _producerTestConfigurations, _networkConfigurations, loggerM, networkService1, _dbServerManager);

                // _producer2ManagementService = new SidechainProducerService(sidechainKeeper2, 
                // new PeerConnectionsHandler(networkService2, sidechainKeeper2, _systemConfig2, loggerP), 
                // _producerTestConfigurations, _networkConfigurations, loggerM, networkService1, _dbServerManager);

                // _producer3ManagementService = new SidechainProducerService(sidechainKeeper3, 
                // new PeerConnectionsHandler(networkService3, sidechainKeeper3, _systemConfig3, loggerP), 
                // _producerTestConfigurations, _networkConfigurations, loggerM, networkService1, _dbServerManager);

                // networkService1.Run();
                // networkService2.Run();
                // networkService3.Run();
                return true;

            }

            return false;
        }
    }
}