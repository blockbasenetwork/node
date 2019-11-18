using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    public class StartProducerCommand : IConfigurationCommand
    {
        public string[] Args { get; set; }
        public SystemConfig SystemConfig { get; set; }
        public bool HasBeenExecuted { get; set; }

        public StartProducerCommand(string[] args, SystemConfig config)
        {
            Args = args;
            SystemConfig = config;
        }

        public async Task ExecuteAsync()
        {
            if (!HasBeenExecuted)
            {
                // var runtimeBuilder = new RuntimeBuilder();

                // var runtime = runtimeBuilder
                //         //.ConfigureMainSettings(Args, SystemConfig)
                //         .ConfigureNetworkService()
                //         .ConfigureSidechainService()
                //         .ConfigureBlockchainBuilder()
                //         .ConfigureHostService()
                //         .Build();

                // SystemConfig.ServiceProvider = runtime.Services;

                // var task = runtime.StartAsync();

                // Task.WaitAll(task);

                // SystemConfig.HasSystemStarted = true;
            }
            else
            {
                //error message
            }

            HasBeenExecuted = true;
        }

        public string GetCommandHelp()
        {
            return "start";
        }

        public bool TryParseCommand(string commandStr)
        {
            return commandStr == "start";
        }
    }
}