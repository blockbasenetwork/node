using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataPersistence.Utils;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO.Analysis;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Rounting;
using BlockBase.Node;
using BlockBase.Runtime;
using BlockBase.Runtime.Mainchain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.IO;
using System.Net;

namespace BlockBase.Api
{
    public class ApiWebHostBuilder
    {
        private readonly IWebHostBuilder _webHostBuider;

        public ApiWebHostBuilder(string[] args)
        {
            _webHostBuider = WebHost.CreateDefaultBuilder(args);
        }

        public ApiWebHostBuilder ConfigureMainSettings(string[] args)
        {
            _webHostBuider.UseStartup<Startup>();
            _webHostBuider.UseSerilog();

            _webHostBuider.ConfigureAppConfiguration((hostingContext, config) =>
             {
                 config.AddEnvironmentVariables();

                 if (args != null)
                 {
                     config.AddCommandLine(args);
                 }
             });

            _webHostBuider.ConfigureServices((hostContext, services) =>
            {
                var env = hostContext.HostingEnvironment;

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                services.Configure<NetworkConfigurations>(configuration.GetSection("NetworkConfigurations"));
                services.Configure<NodeConfigurations>(configuration.GetSection("NodeConfigurations"));
                services.Configure<RequesterConfigurations>(configuration.GetSection("RequesterConfigurations"));
                services.Configure<SidechainPhasesTimesConfigurations>(configuration.GetSection("SidechainPhasesTimesConfigurations"));
                services.Configure<SecurityConfigurations>(configuration.GetSection("SecurityConfigurations"));
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
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("providerApi", new OpenApiInfo { Title = "Service Provider API", Version = "v0.1" });
                    c.SwaggerDoc("requesterApi", new OpenApiInfo { Title = "Service Requester API", Version = "v0.1" });
                    c.SwaggerDoc("networkApi", new OpenApiInfo { Title = "Network API", Version = "v0.1" });
                    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                    c.EnableAnnotations();
                });
            });

            _webHostBuider.ConfigureLogging((logging) =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddEnvironmentVariables()
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration.GetSection("Logging"))
                    .Enrich.FromLogContext()
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                    .WriteTo.File($"logs/ProducerD_{DateTime.UtcNow.ToString("yyyyMMdd-HHmm")}.log")
                    .CreateLogger();

                logging.AddSerilog();
            });

            return this;
        }

        public ApiWebHostBuilder ConfigureNetworkService()
        {
            _webHostBuider.ConfigureServices((hostContext, services) =>
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

        public ApiWebHostBuilder ConfigureSidechainService()
        {
            _webHostBuider.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IConnector, PSqlConnector>();
                services.AddSingleton<ConcurrentVariables>();
                services.AddSingleton<BlockSender>();
                services.AddSingleton<TransactionSender>();
                services.AddSingleton<ISidechainProducerService, SidechainProducerService>();
                services.AddSingleton<SidechainMaintainerManager>();
                services.AddSingleton<PeerConnectionsHandler>();
                services.AddSingleton<SidechainKeeper>();
                services.AddSingleton<BlockValidator>();
                services.AddSingleton<TransactionValidator>();
                services.AddSingleton<DatabaseKeyManager>();                
                services.AddSingleton<IMongoDbProducerService, MongoDbProducerService>();
                services.AddSingleton<IConnectionsChecker, ConnectionsChecker>();
            });

            return this;
        }

        public IWebHostBuilder GetBuilder() => _webHostBuider;
    }
}