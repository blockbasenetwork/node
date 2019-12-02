using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO.Analysis;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Rounting;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BlockBase.TestsConsole
{
    public class RuntimeBuilder
    {
        private readonly IHostBuilder _hostBuider;

        public RuntimeBuilder()
        {
            _hostBuider = new HostBuilder();
        }

        public RuntimeBuilder ConfigureMainSettings(string[] args)
        {
            var appsettings = "appsettings" + (args.Length > 0 ? args[0] : string.Empty) + ".json";

            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(appsettings, false)
            .AddEnvironmentVariables()
            .Build();

            _hostBuider.ConfigureAppConfiguration((hostingContext, config) =>
             {
                 config.AddEnvironmentVariables();

                 if (args != null)
                 {
                     config.AddCommandLine(args);
                 }
             });

            _hostBuider.ConfigureServices((hostContext, services) =>
            {
                services.Configure<ProducerTestConfigurations>(configuration.GetSection("ProducerTestConfigurations"));
                services.Configure<NetworkConfigurations>(configuration.GetSection("NetworkConfigurations"));
                services.Configure<NodeConfigurations>(configuration.GetSection("NodeConfigurations"));
                services.AddOptions();
                services.AddSingleton<SystemConfig>(s =>
                    new SystemConfig(
                        IPAddress.Parse(s.GetRequiredService<IOptions<NetworkConfigurations>>().Value.LocalIpAddress),
                        s.GetRequiredService<IOptions<NetworkConfigurations>>().Value.LocalTcpPort
                    )
                );
                services.Configure<HostOptions>(option =>
                {
                    option.ShutdownTimeout = System.TimeSpan.FromSeconds(3);
                });
            });

            _hostBuider.ConfigureLogging((logging) =>
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File($"logs/ProducerD_{DateTime.UtcNow.ToString("yyyyMMdd-HHmm")}.log")
                    .CreateLogger();

                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddSerilog();
            });

            return this;
        }

        public RuntimeBuilder ConfigureNetworkService()
        {
            _hostBuider.ConfigureServices((hostContext, services) =>
             {
                 services.AddSingleton<INetworkService, NetworkService>();
                 services.AddSingleton<IMainchainService, MainchainService>();
                 services.AddSingleton<MessageSender>();
                 services.AddSingleton<MessageReceiver>();
                 services.AddSingleton<MessageAnalyser>();
                 services.AddSingleton<MessageForwarder>();
                 services.AddSingleton<TcpConnector>();
                 services.AddSingleton<UdpConnector>();
             });

            return this;
        }

        public RuntimeBuilder ConfigureSidechainService()
        {
            _hostBuider.ConfigureServices((hostContext, services) =>
            {
                //services.AddSingleton<KeyBag>();
                //services.AddSingleton<ISidechainService, SidechainService>();
                services.AddSingleton<BlockSender>();
                services.AddSingleton<ISidechainProducerService, SidechainProducerService>();
                services.AddSingleton<PeerConnectionsHandler>();
                services.AddSingleton<SidechainKeeper>();
                services.AddSingleton<BlockValidator>();
                services.AddSingleton<TransactionValidator>();
                services.AddSingleton<IMongoDbProducerService, MongoDbProducerService>();
            });

            return this;
        }

        public RuntimeBuilder ConfigureHostService()
        {
            _hostBuider.ConfigureServices((hostContext, services) =>
            {
                services.Configure<DaemonConfig>(hostContext.Configuration.GetSection("Daemon"));
                services.AddSingleton<IHostedService, ProducerNode>();
            });

            return this;
        }

        public async Task RunAsync()
        {
            await _hostBuider.RunConsoleAsync();
        }

        public IHost Build()
        {
            return _hostBuider.Build();
        }
    }
}