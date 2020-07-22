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
using BlockBase.Domain.Results;
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
    public class GetPeerConnectionStateCommand : AbstractCommand
    {
        private PeerConnectionsHandler _peerConnectionsHandler;

        private ILogger _logger;


        public override string CommandName => "Get peer connection state";

        public override string CommandInfo => "Checks the node connections and returns a list of peers this node currently has knowledge of and the connectino state with each of them";

        public override string CommandUsage => "get peers";

        public GetPeerConnectionStateCommand(ILogger logger, PeerConnectionsHandler peerConnectionsHandler)
        {
            _peerConnectionsHandler = peerConnectionsHandler;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var peers = await _peerConnectionsHandler.PingAllConnectionsAndReturnAliveState();

                if (!peers.Any())
                    return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "No peers found"));

                var peersResult = new List<PeerConnectionResult>();

                foreach (var peer in peers)
                {
                    peersResult.Add(new PeerConnectionResult()
                    {
                        Name = peer.peer.ConnectionAccountName,
                        State = peer.peer.ConnectionState.ToString(),
                        Endpoint = peer.peer.IPEndPoint.ToString(),
                        ConnectionAlive = peer.connectionAlive
                    });
                }

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<PeerConnectionResult>>(peersResult));

            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);

            return new CommandParseResult(true, CommandUsage);
        }
    }
}