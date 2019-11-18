using BlockBase.Network.IO;
using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using BlockBase.Network.IO.Enums;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    class StressTestSenderCommand : IExecutionCommand
    {
        IPAddress IPAddress { get; set; }
        int TcpPort { get; set; }
        //int UdpPort { get; set; }
        int NumberOfMessages { get; set; }
        int SizeOfMessages { get; set; }
        SystemConfig SystemConfig { get; set; }
        IServiceProvider ServiceProvider { get; set; }
        Random Random { get; set; }

        public StressTestSenderCommand(SystemConfig config, IServiceProvider serviceProvider) {
            SystemConfig = config;
            ServiceProvider = serviceProvider;
            Random = new Random();
        }

        public async Task ExecuteAsync()
        {
            var NetworkService = ServiceProvider.GetService<INetworkService>();

            // var count = 0;

            var currentTime = DateTime.Now;

            // for (int i = 0; i < NumberOfMessages; i++) {

            //     byte[] payload = new byte[SizeOfMessages];
            //     Random.NextBytes(payload);
                //byte[] payload = new byte[1300];

            //     var message = new NetworkMessage(NetworkMessageTypeEnum.SendBlockHeaders, payload, TransportTypeEnum.Tcp, new IPEndPoint(IPAddress, TcpPort));

            //     Task.WaitAll(NetworkService.SendMessageAsync(message));
            //     //Console.WriteLine("Message sent count:" + ++count);
            //     ++count;
            //     if (count == 1000)
            //     {
            //         Console.WriteLine("1 000 messages sent in:" + (DateTime.Now - currentTime).TotalSeconds + " seconds");
            //     }
            //     if (count == 10000)
            //     {
            //         Console.WriteLine("10 000 messages sent in:" + (DateTime.Now - currentTime).TotalSeconds + " seconds");
            //     }
            //     if (count == 100000)
            //     {
            //         Console.WriteLine("100 000 messages sent in:" + (DateTime.Now - currentTime).TotalSeconds + " seconds");
            //     }
            //     if (count == 1000000)
            //     {
            //         Console.WriteLine("1 000 000 messages sent in:" + (DateTime.Now - currentTime).TotalSeconds + " seconds");
            //     }
            // }
        }

        public string GetCommandHelp()
        {
            return "sts <ip> <tcp port> <number of messages> <message size>";
            //return "sts <ip> <tcp port> <number of messages>";
        }

        public bool TryParseCommand(string commandStr)
        {
            try
            {
                var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (commandData.Length != 5) return false;
                //if (commandData.Length != 4) return false;

                if (commandData[0] != "sts") return false;

                if (!IPAddress.TryParse(commandData[1], out var ipAddress)) return false;
                IPAddress = ipAddress;

                if (!int.TryParse(commandData[2], out var tcpPort)) return false;
                TcpPort = tcpPort;

                if (!int.TryParse(commandData[3], out var numberOfMessages)) return false;
                NumberOfMessages = numberOfMessages;

                if (!int.TryParse(commandData[4], out var sizeOfMessages)) return false;
                SizeOfMessages = sizeOfMessages;

                return true;
            }
            catch { return false; }

        }
    }
}
