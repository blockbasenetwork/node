using BlockBase.DataPersistence.Data;
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
using BlockBase.Runtime.Requester;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Runtime.Sql;
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
using BlockBase.Node.Filters;
using BlockBase.Runtime.Provider.AutomaticProduction;
using Serilog.Events;
using System.Linq;
using BlockBase.Node.Commands.Requester;

namespace BlockBase.Api
{
    public class ApiWebHostBuilder
    {
        private readonly IWebHostBuilder _webHostBuider;
        private bool _verbose = false;

        public ApiWebHostBuilder(string[] args)
        {
            _webHostBuider = WebHost.CreateDefaultBuilder(args);
            _verbose = args.Any(a => a == "--verbose");
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
                services.Configure<ProviderConfigurations>(configuration.GetSection("ProviderConfigurations"));
                services.Configure<ApiSecurityConfigurations>(configuration.GetSection("ApiSecurityConfigurations"));
                services.AddOptions();

                Func<string, IPAddress> simpleParse = (ipAddressString) =>
                {
                    IPAddress ipAddress;
                    if (!IPAddress.TryParse(ipAddressString, out ipAddress))
                    {
                        var addressList = Dns.GetHostEntry(ipAddressString)?.AddressList;
                        if (addressList != null && addressList.Length > 0) ipAddress = addressList[0];
                    }
                    return ipAddress;
                };

                services.AddSingleton<SystemConfig>(s =>
                    new SystemConfig(
                        simpleParse(s.GetRequiredService<IOptions<NetworkConfigurations>>().Value.PublicIpAddress),
                        s.GetRequiredService<IOptions<NetworkConfigurations>>().Value.TcpPort
                    )
                );
                services.Configure<HostOptions>(option =>
                {
                    option.ShutdownTimeout = System.TimeSpan.FromSeconds(3);
                });
                
                var node = configuration.GetSection("NodeConfigurations").GetValue<string>("AccountName");
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("providerApi", new OpenApiInfo { Title = "Service Provider API", Version = "v0.1", Description = $"Node Account: {node}" });
                    c.SwaggerDoc("requesterApi", new OpenApiInfo { Title = "Service Requester API", Version = "v0.1", Description = $"Node Account: {node}" });
                    c.SwaggerDoc("networkApi", new OpenApiInfo { Title = "Network API", Version = "v0.1", Description = $"Node Account: {node}" });
                    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                    c.EnableAnnotations();

                    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                    {
                        Description = "Api key needed to access the endpoints. X-Api-Key: My_API_Key",
                        In = ParameterLocation.Header,
                        Name = "ApiKey",
                        Type = SecuritySchemeType.ApiKey
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Name = "ApiKey",
                                Type = SecuritySchemeType.ApiKey,
                                In = ParameterLocation.Header,
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                },
                            },
                            new string[] {}
                        }
                    });
                });
            });

            _webHostBuider.ConfigureLogging((logging) =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddEnvironmentVariables()
                    .Build();

                var logConfig = new LoggerConfiguration()
                   .ReadFrom.Configuration(configuration.GetSection("Logging"))
                   .Enrich.FromLogContext();

                if (_verbose)
                {
                    logConfig = logConfig.WriteTo.Console(theme: AnsiConsoleTheme.Code);
                }
                else
                {
                    logConfig = logConfig.WriteTo.Console(theme: AnsiConsoleTheme.Code, restrictedToMinimumLevel: LogEventLevel.Information);
                }

                logConfig = logConfig.WriteTo.File($"logs/BlockBaseNode_.log", rollingInterval: RollingInterval.Day);

                Log.Logger = logConfig.CreateLogger();

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
                 services.AddSingleton<TcpConnectionTester>();
             });

            return this;
        }

        public ApiWebHostBuilder ConfigureSidechainService()
        {
            _webHostBuider.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<ConfigurationChecker>();

                services.AddSingleton<IConnector, PSqlConnector>();
                services.AddSingleton<ConcurrentVariables>();
                services.AddSingleton<BlockRequestsHandler>();
                services.AddSingleton<TransactionsManager>();
                services.AddSingleton<PeerConnectionsHandler>();
                services.AddSingleton<PendingTransactionRecovery>();
                services.AddSingleton<BlockValidationsHandler>();
                services.AddSingleton<TransactionValidationsHandler>();
                services.AddSingleton<DatabaseKeyManager>();
                services.AddSingleton<IMongoDbProducerService, MongoDbProducerService>();
                services.AddSingleton<IMongoDbRequesterService, MongoDbRequesterService>();
                services.AddSingleton<IConnectionsChecker, ConnectionsChecker>();

                services.AddSingleton<ISidechainMaintainerManager, SidechainMaintainerManager>();
                services.AddSingleton<ISidechainProducerService, SidechainProducerService>();
                services.AddSingleton<SidechainKeeper>();

                services.AddSingleton<IAutomaticProductionManager, AutomaticProductionManager>();
            });

            return this;
        }

        

        public ApiWebHostBuilder ConfigureApiSecurity()
        {
            _webHostBuider.ConfigureServices((hostContext, services) =>
            {
                services.AddScoped<ApiKeyAttribute>();
            });

            return this;
        }

        public IWebHostBuilder GetBuilder() => _webHostBuider;
    }
}