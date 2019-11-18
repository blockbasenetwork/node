using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;

namespace Blockbase.ProducerD.Commands
{
    public class StressTestReceiverCommand : IExecutionCommand
    {
        private SystemConfig SystemConfig { get; set; }
        private IServiceProvider ServiceProvider { get; set; }
        DateTime TimeOfBeginning { get; set; }

        public StressTestReceiverCommand(SystemConfig config, IServiceProvider serviceProvider)
        {
            SystemConfig = config;
            ServiceProvider = serviceProvider;
        }

        public async Task ExecuteAsync()
        {
            await Task.Run(() =>
            {
                var networkService = ServiceProvider.GetService<INetworkService>();

                var counter = 0;

                while (true)
                {
                    var task = networkService.ReceiveMessage(NetworkMessageTypeEnum.SendBlockHeaders);

                    Task.WaitAll(task);

                    var opResult = task.Result;

                    if (opResult != null)
                    {
                        if (counter == 0)
                        {
                            TimeOfBeginning = DateTime.Now;
                        }
                        
                        counter++;
                        PrintTime(counter);
                        //Console.WriteLine("Message received:" + ++counter);
                        var message = opResult.Result;
                        opResult = null;
                    }
                }
            });
        }
        public void PrintTime(int counter) {

            var timePassed = DateTime.Now.Subtract(TimeOfBeginning).TotalSeconds;

            if (counter == 10)
            {
                Console.WriteLine("10 messages: " + timePassed + " seconds");
            }

            if (counter == 100)
            {
                Console.WriteLine("100 messages: " + timePassed + " seconds");
            }

            if (counter == 1000)
            {
                Console.WriteLine("1000 messages: " + timePassed + " seconds");
            }

            if (counter == 10000)
            {
                Console.WriteLine("10000 messages: " + timePassed + " seconds");
            }

            if (counter == 100000)
            {
                Console.WriteLine("100000 messages: " + timePassed + " seconds");
            }

            if (counter == 1000000)
            {
                Console.WriteLine("1000000 messages: " + timePassed + " seconds");
            }

        }
        public string GetCommandHelp()
        {
            return "str";
        }

        public bool TryParseCommand(string commandStr)
        {
            try
            {
                var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (commandData.Length != 1) return false;

                if (commandData[0] != "str") return false;

                return true;
            }
            catch { return false; }
        }
    }
}