using BlockBase.Domain.Configurations;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO.Analysis;
using BlockBase.Network.Rounting;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using System;
using Serilog.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using BlockBase.Node;
using Microsoft.Extensions.Options;
using System.Net;
using BlockBase.DataPersistence;
using BlockBase.Network.Mainchain;
using BlockBase.Network.History;
using BlockBase.DataPersistence.ProducerData;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.OpenApi.Models;

namespace Blockbase.Api
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
                services.Configure<MongoDBConfigurations>(configuration.GetSection("MongoDBConfigurations"));
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
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Producer API", Version = "v1" });

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
                 services.AddSingleton<IHistoryService, HistoryService>();
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

        public IWebHostBuilder GetBuilder() => _webHostBuider;
    }
}