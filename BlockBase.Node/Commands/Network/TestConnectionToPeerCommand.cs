using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Network
{
    public class TestConnectionToPeerCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private TcpConnectionTester _tcpConnectionTester;
        private string _ipAddress;
        private int _port;

        private ILogger _logger;


        public override string CommandName => "Test connection to peer";

        public override string CommandInfo => "Tests connection to peer with specified address and port";

        public override string CommandUsage => "test conn --ip <ipAddress> --p <port>";

        public TestConnectionToPeerCommand(ILogger logger, IMainchainService mainchainService, TcpConnectionTester tcpConnectionTester)
        {
            _tcpConnectionTester = tcpConnectionTester;
            _mainchainService = mainchainService;
            _logger = logger;
        }

         public TestConnectionToPeerCommand(ILogger logger, IMainchainService mainchainService, TcpConnectionTester tcpConnectionTester, string ipAddress, int port) : this(logger, mainchainService, tcpConnectionTester)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (!IPAddress.TryParse(_ipAddress, out var ipAddr)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Unable to parse the ipAddress"));

                var ipEndPoint = new IPEndPoint(ipAddr, _port);
                var peer = await _tcpConnectionTester.TestListen(ipEndPoint);
                if (peer != null)
                    return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Tried to establish connection to peer. Check the console for results."));
                else
                    return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, $"Unable to connect to peer"));

            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 6;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("test conn");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 6)
            {
                if (commandData[2] != "--ip") return new CommandParseResult(true, CommandUsage);
                _ipAddress = commandData[3];

                if (commandData[4] != "--p") return new CommandParseResult(true, CommandUsage);
                if (!Int32.TryParse(commandData[5], out int port)) return new CommandParseResult(true, "Couldn't parse the port.");
                _port = port;
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}