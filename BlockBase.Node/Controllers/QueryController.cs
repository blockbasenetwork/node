using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Pocos;
using BlockBase.Domain.Results;
using BlockBase.Runtime;
using BlockBase.Runtime.Sidechain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "requesterApi")]
    public class QueryController : ControllerBase
    {
        private SqlCommandManager _sqlCommandManager;
        private DatabaseKeyManager _databaseKeyManager;
        public QueryController(ILogger<QueryController> logger, DatabaseKeyManager databaseKeyManager, IConnector psqlConnector, ConcurrentVariables concurrentVariables, TransactionSender transactionSender, IOptions<NodeConfigurations> nodeConfigurations, IMongoDbProducerService mongoDbProducerService)
        {
            _databaseKeyManager = databaseKeyManager;
            psqlConnector.Setup().Wait();
            //transactionSender.Setup().Wait();
            _sqlCommandManager = new SqlCommandManager(new MiddleMan(databaseKeyManager), logger, psqlConnector, concurrentVariables, transactionSender, nodeConfigurations.Value, mongoDbProducerService);
        }


        /// <summary>
        /// Sends a query to be executed
        /// </summary>
        /// <param name="queryScript">The query to execute</param>
        /// <returns> Success or list of results </returns>
        /// <response code="200">Query executed with success</response>
        /// <response code="400">Query invalid</response>
        /// <response code="500">Error executing query</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends query to be executed",
            Description = "The requester uses this service to create databases, update them and delete them",
            OperationId = "ExecuteQuery"
        )]
        public async Task<ObjectResult> ExecuteQuery([FromBody]string queryScript)
        {
            try
            {
                CheckIfSecretIsSet();
                var queryResults = await _sqlCommandManager.Execute(queryScript);

                return Ok(new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<QueryResult>>(e));
            }
        }

        /// <summary>
        /// Sends a query to get all the values from a certain table in a certain database
        /// </summary>
        /// <returns> Success or list of results </returns>
        /// <response code="200">Query executed with success</response>
        /// <response code="400">Query invalid</response>
        /// <response code="500">Error executing query</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Sends a query to get all the values from a certain table in a certain database",
            Description = "The requester uses this service to see all values encrypted or not from a certain table",
            OperationId = "GetAllTableValues"
        )]
        public async Task<ObjectResult> GetAllTableValues([FromBody] SidebarQueryInfo sidebarQueryInfo)
        {
            try
            {
                CheckIfSecretIsSet();
                var query = $"USE {sidebarQueryInfo.DatabaseName}; SELECT {sidebarQueryInfo.TableName}.* FROM {sidebarQueryInfo.TableName}";
                if (sidebarQueryInfo.Encrypted) query += " ENCRYPTED";
                query += ";";

                var queryResults = await _sqlCommandManager.Execute(query);

                return Ok(new OperationResponse<IList<QueryResult>>(queryResults));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<QueryResult>>(e));
            }
        }

        /// <summary>
        /// Asks for databases, tables and columns structure
        /// </summary>
        /// <returns> Structure of databases </returns>
        /// <response code="200">Structure retrieved with success</response>
        /// <response code="400">Invalid request</response>
        /// <response code="500">Error getting structure information</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Asks for databases, tables and columns structure",
            Description = "The requester uses this service to know databases structure",
            OperationId = "GetStructure"
        )]
        public ObjectResult GetStructure()
        {
            try
            {
                CheckIfSecretIsSet();
                var structure = _sqlCommandManager.GetStructure();
                return Ok(new OperationResponse<IList<DatabasePoco>>(structure));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<DatabasePoco>>(e));
            }
        }

        //TODO rpinto - Consider refactoring. This is just  boolean check with a throw
        private void CheckIfSecretIsSet()
        {
            if (!_databaseKeyManager.DataSynced)
                throw new Exception("Passwords and main key not set.");

        }
        public class SidebarQueryInfo
        {
            public bool Encrypted { get; set; }
            public string DatabaseName { get; set; }
            public string TableName { get; set; }
        }
    }
}