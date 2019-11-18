using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;

namespace Blockbase.ProducerD.Commands
{
    public class ConnectToProducerCommand : IExecutionCommand
    {
        private IPAddress IPAddress { get; set; }
        private int Port { get; set; }
        private INetworkService NetworkService { get; set; }
        private SystemConfig SystemConfig { get; set; }
        private IServiceProvider ServiceProvider { get; set; }

        public ConnectToProducerCommand(SystemConfig config, IServiceProvider serviceProvider)
        {
            SystemConfig = config;
            ServiceProvider = serviceProvider;
        }

        public async Task ExecuteAsync()
        {
            await NetworkService.ConnectAsync(new IPEndPoint(IPAddress, Port));

            //byte[] payload = Encoding.ASCII.GetBytes("publickey");
            //var message = new NetworkMessage(NetworkMessageTypeEnum.RequestProducerIdentification, payload, TransportTypeEnum.Tcp, new IPEndPoint(IPAddress, Port));
            //NetworkService.SendMessageAsync(message);
            //Console.WriteLine(DateTime.UtcNow + ": message sent");

            //var task = NetworkService.ReceiveMessage(NetworkMessageTypeEnum.RequestProducerIdentification);
            //Task.WaitAll(task);
            //var opResult = task.Result;
        }

        public string GetCommandHelp()
        {
            return "cnt <ip> <port>";
        }

        public bool TryParseCommand(string commandStr)
        {
            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandData.Length != 3) return false;
            if (commandData[0] != "cnt") return false;

            if (!IPAddress.TryParse(commandData[1], out var ipAddress)) return false;
            if (!int.TryParse(commandData[2], out var port)) return false;

            IPAddress = ipAddress;
            Port = port;

            NetworkService = ServiceProvider.GetService<INetworkService>();

            return true;
        }
    }
}