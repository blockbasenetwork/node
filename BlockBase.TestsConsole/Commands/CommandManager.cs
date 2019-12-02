using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using BlockBase.Runtime.Network;
using BlockBase.TestsConsole.Commands.Interfaces;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.TestsConsole.Commands
{
    public class CommandManager
    {
        private readonly ILogger _logger;
        private readonly SystemConfig _systemConfig;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMongoDbProducerService _mongoDbProducerService;
        private readonly ProducerTestConfigurations _producerTestConfigurations;
        private readonly NetworkConfigurations _networkTestConfigurations;
        private readonly IOptions<ProducerTestConfigurations> _optionsProducerTestConfigurations;
        private readonly IOptions<NetworkConfigurations> _optionsNetworkConfigurations;
        private readonly INetworkService _networkService;
        private readonly NodeConfigurations _nodeConfigurations;
        private CommandStorageHelper _commandStorageHelper = new CommandStorageHelper("savedCommands.txt");

        public CommandManager(IOptions<ProducerTestConfigurations> producerTestConfigurations, IOptions<NetworkConfigurations> networkConfigurations, ILogger logger, SystemConfig systemConfig, INetworkService networkService, IServiceProvider serviceProvider, IMongoDbProducerService mongoDbProducerService, NodeConfigurations nodeConfigurations)
        {
            _optionsProducerTestConfigurations = producerTestConfigurations;
            _optionsNetworkConfigurations = networkConfigurations;
            _producerTestConfigurations = producerTestConfigurations?.Value;
            _networkTestConfigurations = networkConfigurations?.Value;
            _logger = logger;
            _systemConfig = systemConfig;
            _networkService = networkService;
            _serviceProvider = serviceProvider;
            _mongoDbProducerService = mongoDbProducerService;
            _nodeConfigurations = nodeConfigurations;
        }

        public async Task RunAsync()
        {
            var commands = LoadCommands(_systemConfig);

            while (true)
            {
                //systemConfig.HasSystemStarted = true; //bruno.pires - For test purposes. Remove after testing.
                // var availableCommands = _systemConfig.HasSystemStarted
                //     ? commands.Where(c => c is IExecutionCommand || c is IHelperCommand)
                //     : commands.Where(c => c is IConfigurationCommand || c is IHelperCommand);

                Console.Write("> ");
                string input = Console.ReadLine();

                if (input == null) continue;

                if (input == "quit") return;

                input = TryTransformInputCommand(input);

                foreach (var command in commands)
                {
                    if (command.TryParseCommand(input))
                    {
                        await command.ExecuteAsync();
                        _commandStorageHelper.SaveInputCommand(input);
                        break;
                    }
                }
            }
        }

        private IList<ICommand> LoadCommands(SystemConfig config)
        {
            var commands = new List<ICommand>
            {
                //new ConfigureNetworkServiceCommand(config, _logger, _networkService),
                //new StartProducerCommand(args, config),
                //new ConnectToProducerCommand(config, _serviceProvider),
                //new StressTestSenderCommand(config, _serviceProvider),
                //new StressTestReceiverCommand(config, _serviceProvider),
                //new ConnectToDatabaseCommand(config),
                //new ManageDatabaseCommand(config),
                //new TestContractConnectionCommand(config, _producerTestConfigurations, _logger, _networkTestConfigurations, _serviceProvider),
                //new RunProducerCommand(config, _producerTestConfigurations, _networkTestConfigurations, _nodeConfigurations, _logger, _serviceProvider, _mongoDbProducerService),
                //new DatabaseTesterCommand(_logger),
                //new MultiSigTestCommand(_producerTestConfigurations, _networkTestConfigurations),
                //new AllTestCommand(_logger, config, _optionsNetworkConfigurations, _optionsProducerTestConfigurations, _serviceProvider, _mongoDbProducerService),
                //new RecoverCommand(config, _producerTestConfigurations, _networkTestConfigurations, _logger, _serviceProvider, _mongoDbProducerService),
                //new ECCTestCommand(_logger),
                //new SendTransactionsCommand(_producerTestConfigurations, _logger, _serviceProvider),
                //new ContractManagerCommand(config, _networkTestConfigurations, _serviceProvider),
                //new TestBareBonesSqlCommand(_logger),
                //new GetDatabaseStructureCommand(),
                new TestTransformerCommand(_logger)
            };
            return commands;
        }

        private string TryTransformInputCommand(string commandStr)
        {
            if (commandStr == "csc")
            {
                _commandStorageHelper.ClearStorageCommand();
                return commandStr;
            }

            if (commandStr != "cfec") return commandStr;
            var commands = _commandStorageHelper.LoadInputCommands();

            if (commands.Count > 0)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    Console.WriteLine(i + ": " + commands[i]);
                }
                Console.Write("Choose command: ");
                var input = Console.ReadLine();

                if (!int.TryParse(input, out var commandNumber)) return commandStr;
                if (commandNumber < 0 || commandNumber > commands.Count - 1) return commandStr;

                return commands[commandNumber];
            }

            Console.WriteLine("There's no commands stored.");
            return commandStr;
        }
    }
}