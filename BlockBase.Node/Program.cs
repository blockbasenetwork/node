using BlockBase.Api;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Runtime.Provider.AutomaticProduction;
using BlockBase.Runtime.Requester;
using BlockBase.Utils.Extensions;
using BlockBase.Utils.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BlockBase.Node
{
    public class Program
    {
        public static void Main(string[] args)
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
            if(!databasesPrefixCheck) return;
            

            if(noRecoverCommandFound == false)
            {
                Task.WaitAll(pendingTransactionRecovery.Run());
                Task.WaitAll(sidechainProducerService.Run());
                automaticProductionManager.Start();
            }

            //force instantiation of the connection tester
            var connectionTester = webHost.Services.Get<TcpConnectionTester>();




            webHost.Run();
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
    }
}