using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Pocos;
using BlockBase.Domain.Results;
using BlockBase.Runtime;
using BlockBase.Runtime.Network;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using Wiry.Base32;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "requesterApi")]
    public class QueryController : ControllerBase
    {
        private SqlCommandManager _sqlCommandManager;
        public QueryController(ILogger<QueryController> logger, DatabaseKeyManager databaseKeyManager, IConnector psqlConnector)
        {
            _sqlCommandManager = new SqlCommandManager(new MiddleMan(databaseKeyManager), logger, psqlConnector);
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
                var query = $"USE {sidebarQueryInfo.DatabaseName}; SELECT {sidebarQueryInfo.TableName}.* FROM {sidebarQueryInfo.TableName}";
                if(sidebarQueryInfo.Encrypted) query += " ENCRYPTED";
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
                var structure = _sqlCommandManager.GetStructure();
                return Ok(new OperationResponse<IList<DatabasePoco>>(structure));
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new OperationResponse<IList<DatabasePoco>>(e));
            }
        }
        public class SidebarQueryInfo
    {
        public bool Encrypted {get; set;}
        public string DatabaseName {get; set;}
        public string TableName {get; set;}
    }
    }
}