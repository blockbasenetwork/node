using System;
using System.IO;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Runtime;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace Blockbase.ProducerD
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