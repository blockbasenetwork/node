using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using BlockBase.Extensions;
using BlockBase.Runtime.Network;
using BlockBase.TestsConsole.Commands;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlockBase.TestsConsole
{
    public class ProducerNode : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<DaemonConfig> _config;
        private readonly TaskContainer _commandManagerTask;
        private readonly IMongoDbProducerService _mongoDbProducerService;
        private readonly NodeConfigurations _nodeConfigurations;

        public ProducerNode(IServiceProvider serviceProvider, ILogger<ProducerNode> logger, IOptions<DaemonConfig> config, IMongoDbProducerService mongoDbProducerService, IOptions<NodeConfigurations> nodeConfigurations)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _config = config;
            _mongoDbProducerService = mongoDbProducerService;
            _nodeConfigurations = nodeConfigurations.Value;

            var systemConfig = _serviceProvider.Get<SystemConfig>();

            var networkService = _serviceProvider.Get<INetworkService>();

            var commandManager = new CommandManager(_serviceProvider.Get<IOptions<ProducerTestConfigurations>>(), _serviceProvider.Get<IOptions<NetworkConfigurations>>(), _logger, systemConfig, networkService, _serviceProvider, _mongoDbProducerService, _nodeConfigurations);
            _commandManagerTask = TaskContainer.Create(async () => await commandManager.RunAsync(), typeof(CommandManager).ToString());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting daemon: " + _config.Value.DaemonName);

            _commandManagerTask.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping daemon.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing....");
        }
    }
}