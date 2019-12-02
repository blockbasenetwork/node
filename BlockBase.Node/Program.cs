using BlockBase.Api;
using BlockBase.Extensions;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.SidechainProducer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace BlockBase.Node
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHost = CreateWebHostBuilder(args).Build();

            var networkService = webHost.Services.Get<INetworkService>();
            var sidechainProducerService = webHost.Services.Get<ISidechainProducerService>();

            var noRecover = args.FirstOrDefault() == "--no-recover";

            networkService.Run();
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
                    .GetBuilder();

            return builder;
        }
    }
}