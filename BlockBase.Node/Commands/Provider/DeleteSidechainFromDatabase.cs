using System;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using Microsoft.Extensions.Logging;
namespace BlockBase.Node.Commands.Provider
{
    public class DeleteSidechainFromDatabase : AbstractCommand
    {
        private ISidechainProducerService _sidechainProducerService;

        private IMongoDbProducerService _mongoDbProducerService;

        private ILogger _logger;
        
        private IConnector _connector;

        private string _chainName;

        private bool _force;


        public override string CommandName => "Delete sidechain from database";

        public override string CommandInfo => "Deletes data from specified sidechain";

        public override string CommandUsage => "rm data --chain <sidechainName> --force <true/false>";

        public DeleteSidechainFromDatabase(ILogger logger, ISidechainProducerService sidechainProducerService, IMongoDbProducerService mongoDbProducerService, IConnector psqlConnector)
        {
            _sidechainProducerService = sidechainProducerService;
            _mongoDbProducerService = mongoDbProducerService;
            _logger = logger;
            _connector = psqlConnector;
        }

        public DeleteSidechainFromDatabase(ILogger logger, ISidechainProducerService sidechainProducerService, IMongoDbProducerService mongoDbProducerService, string sidechainName, bool force, IConnector psqlConnector) : this(logger, sidechainProducerService, mongoDbProducerService, psqlConnector)
        {
            _chainName = sidechainName;
            _force = force;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid sidechain name"));
            _chainName = _chainName.Trim();

            try
            {

                var chainExistsInPool = _sidechainProducerService.DoesChainExist(_chainName);

                if (chainExistsInPool && !_force)
                {
                    return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer is still working on producing blocks for sidechain {_chainName}. Consider requesting to leave the sidechain production first. If you're sure, use force=true on the request."));
                }

                if (chainExistsInPool && _force)
                {
                    //if chain exists in pool and isn't running, remove it
                    //this also means that there should be remnants of the database
                    _logger.LogDebug($"Removing sidechain {_chainName} execution engine");
                    _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(_chainName);
                }


                var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_chainName);
                //TODO rpinto - this deletes the whole database - what if a producer leaves production and joins further ahead...?
                if (chainExistsInDb)
                {
                    _logger.LogDebug($"Removing sidechain {_chainName} data from database");
                    await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(_chainName);
                    await _connector.DropSidechainDatabases(_chainName);
                }

                var responseMessage = chainExistsInPool && _force ? "Successfully stopped chain production. " : "Chain not being produced. ";
                responseMessage += chainExistsInDb ? "Successfully removed chain from database." : "Chain not found in database.";


                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, responseMessage));
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
            return commandStr.StartsWith("rm data");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 6) 
            {
                if(commandData[2] != "--chain") return  new CommandParseResult(true, CommandUsage);
                _chainName = commandData[3];
                if(commandData[4] != "--force") return  new CommandParseResult(true, CommandUsage);
                if(!Boolean.TryParse(commandData[5], out bool force)) return  new CommandParseResult(true, "Couldn't parse force value.");
                _force = force;
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}