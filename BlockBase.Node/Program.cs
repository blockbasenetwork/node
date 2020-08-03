using BlockBase.Api;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataPersistence.Utils;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands;
using BlockBase.Node.Commands.Network;
using BlockBase.Node.Commands.Provider;
using BlockBase.Node.Commands.Requester;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Runtime.Provider.AutomaticProduction;
using BlockBase.Runtime.Requester;
using BlockBase.Runtime.Sql;
using BlockBase.Utils.Extensions;
using BlockBase.Utils.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BlockBase.Node
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            Console.WriteLine($"Running version: {Assembly.GetEntryAssembly().GetName().Version.ToString(3)}");

            var webHost = CreateWebHostBuilder(args).Build();

            ServiceProvider.Set(webHost.Services);

            var networkService = webHost.Services.Get<INetworkService>();

            var sidechainMaintainerService = webHost.Services.Get<ISidechainMaintainerManager>();
            var sidechainProducerService = webHost.Services.Get<ISidechainProducerService>();
            var automaticProductionManager = webHost.Services.Get<IAutomaticProductionManager>();
            var pendingTransactionRecovery = webHost.Services.Get<PendingTransactionRecovery>();
            var configurationChecker = webHost.Services.Get<ConfigurationChecker>();
            var mainchainService = webHost.Services.Get<IMainchainService>();
            var peerConnectionsHandler = webHost.Services.Get<PeerConnectionsHandler>();
            var logger = webHost.Services.Get<ILogger<Program>>();
            var tcpConnectionTester = webHost.Services.Get<TcpConnectionTester>();
            var connectionsChecker = webHost.Services.Get<IConnectionsChecker>();
            var networkConfigurations = (webHost.Services.Get<IOptions<NetworkConfigurations>>()).Value;
            var nodeConfigurations = (webHost.Services.Get<IOptions<NodeConfigurations>>()).Value;
            var requesterConfigurations = (webHost.Services.Get<IOptions<RequesterConfigurations>>()).Value;
            var mongoDbProducerService = webHost.Services.Get<IMongoDbProducerService>();
            var mongoDbRequesterService = webHost.Services.Get<IMongoDbRequesterService>();
            var databaseKeyManager = webHost.Services.Get<DatabaseKeyManager>();
            var concurrentVariables = webHost.Services.Get<ConcurrentVariables>();
            var connector = webHost.Services.Get<IConnector>();
            var transactionsManager = webHost.Services.Get<TransactionsManager>();
            var sqlCommandManager = new SqlCommandManager(new MiddleMan(databaseKeyManager), logger, connector, concurrentVariables, transactionsManager, nodeConfigurations, mongoDbRequesterService);



            var commandList = CreateCommandList(logger, mainchainService, peerConnectionsHandler, tcpConnectionTester, connectionsChecker,
            networkConfigurations, nodeConfigurations, requesterConfigurations, sidechainProducerService,
            mongoDbProducerService, sidechainMaintainerService, sqlCommandManager, databaseKeyManager, concurrentVariables, connector);

            var noRecoverCommandFound = args.Where(s => s == "--no-recover").FirstOrDefault() != null;

            networkService.Run();
            //TODO rpinto - commented this because I don't want it to start on startup for now - uncomment when ready
            //sidechainMaintainerService.Start();

            //check keys
            var keyCheck = configurationChecker.CheckKeys();
            Task.WaitAll(keyCheck);
            if (!keyCheck.Result) return;

            //check databases prefix
            var databasesPrefixCheck = configurationChecker.CheckDatabasesPrefix();
            if (!databasesPrefixCheck) return;


            if (noRecoverCommandFound == false)
            {
                Task.WaitAll(pendingTransactionRecovery.Run());
                Task.WaitAll(sidechainProducerService.Run());
                automaticProductionManager.Start();
            }

            //force instantiation of the connection tester
            var connectionTester = webHost.Services.Get<TcpConnectionTester>();

            var t = Task.Run(() => webHost.RunAsync());

            while (true)
            {
                var cmd = Console.ReadLine();
                if(cmd == null) continue;
                foreach (var command in commandList)
                {
                    var loadResult = command.TryLoadCommand(cmd);
                    if (loadResult.Succeeded && loadResult.CommandRecognized)
                    {
                        var result = await command.Execute();
                        var message = result.OperationResponse.ResponseMessage;
                        Console.WriteLine("Http status code: " + result.HttpStatusCode + " Operation Response: " + message);
                        break;
                    }
                }
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var webHostBuilder = new ApiWebHostBuilder(args);

            var builder = webHostBuilder
                    .ConfigureMainSettings(args)
                    .ConfigureNetworkService()
                    .ConfigureSidechainService()
                    .ConfigureApiSecurity()
                    .GetBuilder();

            return builder;
        }

        private static IList<ICommand> CreateCommandList(ILogger logger, IMainchainService mainchainService, PeerConnectionsHandler peerConnectionsHandler, TcpConnectionTester tcpConnectionTester, IConnectionsChecker connectionsChecker, NetworkConfigurations networkConfigurations, NodeConfigurations nodeConfigurations, RequesterConfigurations requesterConfigurations, ISidechainProducerService sidechainProducerService, IMongoDbProducerService mongoDbProducerService, ISidechainMaintainerManager sidechainMaintainerManager, SqlCommandManager sqlCommandManager, DatabaseKeyManager databaseKeyManager, ConcurrentVariables concurrentVariables, IConnector connector)
        {
            return new List<ICommand>()
            {
                new GetAccountStakeRecordsCommand(logger, mainchainService),
                new GetAllBlockBaseSidechainsCommand(logger),
                new GetCurrentUnclaimedRewardsCommand(logger, mainchainService),
                new GetPeerConnectionStateCommand(logger, peerConnectionsHandler),
                new GetProviderCandidatureStateCommand(logger, mainchainService),
                new GetSidechainConfigurationCommand(logger, mainchainService),
                new GetSidechainStateCommand(logger, mainchainService),
                new GetTopProducersEndpointsCommand(logger),
                new TestConnectionToPeerCommand(logger, mainchainService, tcpConnectionTester),
                new RequesterAddStakeCommand(logger, mainchainService, nodeConfigurations), 
                new CheckProviderConfig(logger, mainchainService, nodeConfigurations, networkConfigurations, connectionsChecker),
                new ClaimAllRewardsCommand(logger, mainchainService, nodeConfigurations), 
                new ProviderClaimStakeCommand(logger, mainchainService, nodeConfigurations),
                new DeleteSidechainFromDatabase(logger, sidechainProducerService, mongoDbProducerService), 
                new ProviderGetDecryptedNodeIpsCommand(logger, mainchainService, nodeConfigurations),
                new GetProducingSidechainsCommand(logger, sidechainProducerService, mainchainService, nodeConfigurations),
                new GetSidechainNodeSoftwareVersionCommand(logger, mainchainService),
                new GetTransactionCommand(logger, mongoDbProducerService),
                new GetTransactionsInMempoolCommand(logger, mongoDbProducerService),
                new RemoveCandidatureCommand(logger, mainchainService, nodeConfigurations, sidechainProducerService),
                new RequestToLeaveSidechainProductionCommand(logger, mainchainService, nodeConfigurations, mongoDbProducerService),
                new RequestToProduceSidechainCommand(logger, mainchainService, nodeConfigurations, sidechainProducerService, mongoDbProducerService),
                new AddReservedSeatsCommand(logger, mainchainService, nodeConfigurations), 
                new ProviderAddStakeCommand(logger, mainchainService, nodeConfigurations),
                new CheckCurrentStakeInSidechainCommand(logger, mainchainService, nodeConfigurations, networkConfigurations),
                new CheckRequesterConfig(logger, mainchainService, nodeConfigurations, networkConfigurations, connectionsChecker),
                new CheckSidechainReservedSeatsCommand(logger, mainchainService, nodeConfigurations), 
                new RequesterClaimStakeCommand(logger, mainchainService, nodeConfigurations),
                new EndSidechainCommand(logger, sidechainMaintainerManager, mainchainService, nodeConfigurations, concurrentVariables),
                new ExecuteQueryCommand(logger, databaseKeyManager, sqlCommandManager),
                new GenerateMasterKeyCommand(logger),
                new GetAllTableValuesCommand(logger, databaseKeyManager, sqlCommandManager),
                new RequesterGetDecryptedNodeIpsCommand(logger, mainchainService, nodeConfigurations),
                new GetAllTableValuesCommand(logger, databaseKeyManager, sqlCommandManager),
                new PauseSidechainMaintenanceCommand(logger, sidechainMaintainerManager),
                new RemoveAccountFromBlacklistCommand(logger, mainchainService, nodeConfigurations),
                new RemoveReservedSeatsCommand(logger, mainchainService, nodeConfigurations),
                new RemoveSidechainDatabasesAndKeysCommand(logger, sidechainMaintainerManager, sqlCommandManager),
                new RequestNewSidechainCommand(logger, connector, mainchainService, nodeConfigurations, requesterConfigurations),
                new RunSidechainMaintenanceCommand(logger, sidechainMaintainerManager),
                new SetSecretCommand(logger, requesterConfigurations, databaseKeyManager, connector)
            };
        }
    }
}