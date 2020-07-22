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
using BlockBase.Domain.Endpoints;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Domain.Results;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Network
{
    public class GetAllBlockBaseSidechainsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NetworkType _networkType;

        private ILogger _logger;


        public override string CommandName => "Get all blockbase sidechains";

        public override string CommandInfo => "Retrieves all blockbase sidechains";

        public override string CommandUsage => "get all chain [ --netType <networkType> ]";

        public GetAllBlockBaseSidechainsCommand(ILogger logger, IMainchainService mainchainService)
        {
            _mainchainService = mainchainService;
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var request = HttpHelper.ComposeWebRequestGet(BlockBaseNetworkEndpoints.GET_ALL_TRACKER_SIDECHAINS + $"?network={_networkType.ToString()}");
                var json = await HttpHelper.CallWebRequest(request);
                var trackerSidechains = JsonConvert.DeserializeObject<List<TrackerSidechain>>(json);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<TrackerSidechain>>(trackerSidechains));
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Unable to retrieve the list of sidechains"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 3 || commandData.Length == 5;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("get all chain");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 3)
            {
                _networkType = NetworkType.All;
                return new CommandParseResult(true, true);
            }
            if (commandData.Length == 5)
            {
                if (commandData[3] != "--netType") return new CommandParseResult(true, CommandUsage);
                
                if (commandData[4] == "jungle") _networkType = NetworkType.Jungle;
                else if (commandData[4] == "all") _networkType = NetworkType.All;
                else if (commandData[4] == "mainnet") _networkType = NetworkType.Mainnet;
                else return new CommandParseResult(true, CommandUsage);
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }

    }
}