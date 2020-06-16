using BlockBase.Api;
using BlockBase.Extensions;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Runtime.Requester;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;

namespace BlockBase.Node
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Running version: {Assembly.GetEntryAssembly().GetName().Version.ToString(3)}");

            var webHost = CreateWebHostBuilder(args).Build();

            var networkService = webHost.Services.Get<INetworkService>();

            var sidechainMaintainerService = webHost.Services.Get<ISidechainMaintainerManager>();
            var sidechainProducerService = webHost.Services.Get<ISidechainProducerService>();


            var noRecover = args.Where(s => s == "--no-recover").FirstOrDefault() != null;

            networkService.Run();
                
            sidechainMaintainerService.Start();
            
            sidechainProducerService.Run(!noRecover);

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