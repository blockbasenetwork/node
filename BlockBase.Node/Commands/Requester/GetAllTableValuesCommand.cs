using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.Node.Commands.Utils;
using BlockBase.DataProxy.Encryption;
using System.Collections.Generic;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Sql;
using static BlockBase.Node.Controllers.RequesterController;
using BlockBase.Domain.Requests;

namespace BlockBase.Node.Commands.Requester
{
    public class GetAllTableValuesCommand : AbstractCommand
    {

        private ILogger _logger;
        private DatabaseKeyManager _databaseKeyManager;
        private SqlCommandManager _sqlCommandManager;
        private string _databaseName;
        private string _tableName;
        private bool _encrypted;


        public override string CommandName => "Get all table values";

        public override string CommandInfo => "Gets all table values, encrypted or not";

        public override string CommandUsage => "get table --dbName <databaseName> --tbName <tableName> --encrypted <true/false>";

        public GetAllTableValuesCommand(ILogger logger, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager)
        {
            _logger = logger;
            _databaseKeyManager = databaseKeyManager;
            _sqlCommandManager = sqlCommandManager;
        }

        public GetAllTableValuesCommand(ILogger logger, DatabaseKeyManager databaseKeyManager, SqlCommandManager sqlCommandManager, SidebarQueryInfo sidebarQueryInfo) : this(logger, databaseKeyManager, sqlCommandManager)
        {
            _databaseName = sidebarQueryInfo.DatabaseName;
            _tableName = sidebarQueryInfo.TableName;
            _encrypted = sidebarQueryInfo.Encrypted;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (!_databaseKeyManager.DataSynced) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Passwords and main key not set."));
                var query = $"USE {_databaseName}; SELECT {_tableName}.* FROM {_tableName}";
                if (_encrypted) query += " ENCRYPTED";
                query += ";";

                var queryResults = await _sqlCommandManager.Execute(query);

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 8;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.ToLower().StartsWith("get table");
        }

        protected override CommandParseResult ParseCommand(string[] commandData) 
        {
            if(commandData[2] != "--dbName") return new CommandParseResult(true, CommandUsage);
            _databaseName = commandData[3];
            if(commandData[4] != "--tbName") return new CommandParseResult(true, CommandUsage);
            _tableName = commandData[5];
            if(commandData[6] != "--encrypted") return new CommandParseResult(true, CommandUsage);
            if(!Boolean.TryParse(commandData[7], out bool encrypted)) return new CommandParseResult(true, CommandUsage);
            _encrypted = encrypted;
            return new CommandParseResult(true, true);
        }

    }
}