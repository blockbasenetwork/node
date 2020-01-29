using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Results;
using BlockBase.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace BlockBase.Node.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "requesterApi")]
    public class QueryController : ControllerBase
    {
        private SqlCommandManager _sqlCommandManager;
        public QueryController(ILogger<QueryController> logger)
        {
            var psqlConnector = new PSqlConnector("localhost", "postgres", 5432, "qwerty123", logger);
            var secretStore = new SecretStore();
            
            //TODO - remove this from here
            secretStore.SetSecret("master_key", KeyAndIVGenerator_v2.CreateRandomKey());
            secretStore.SetSecret("master_iv", KeyAndIVGenerator_v2.CreateMasterIV("qwerty123"));

            _sqlCommandManager = new SqlCommandManager(new MiddleMan(new DatabaseKeyManager(secretStore), secretStore), logger, psqlConnector);

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
    }
}