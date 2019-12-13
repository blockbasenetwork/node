using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace BlockBase.TestsConsole
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var runtimeBuilder = new RuntimeBuilder();

            var runtime = runtimeBuilder
                    .ConfigureMainSettings(args)
                    .ConfigureNetworkService()
                    .ConfigureSidechainService()
                    .ConfigureHostService()
                    .Build();

            await runtime.RunAsync();
        }
    }
}