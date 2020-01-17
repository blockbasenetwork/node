using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using Microsoft.Extensions.Logging;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlockBase.Domain.Database.Sql.Generators;
using System.Threading.Tasks;
using BlockBase.Domain.Database.Info;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public class PSqlConnector : IConnector
    {
        private string _serverConnectionString;
        private ILogger _logger;

        private static readonly string INFO_TABLE_NAME = "info";


        public PSqlConnector(string serverName, string user, int port, string password, ILogger logger)
        {
            _serverConnectionString = "Server=" + serverName + ";User ID=" + user + ";Port=" + port + ";Password=" + password;
            _logger = logger;
        }

        public async Task<IList<InfoRecord>> GetInfoRecords(string databaseName)
        {
            var query = $"SELECT row_to_json(result) from (SELECT * FROM {INFO_TABLE_NAME}) AS result;";

            var infoRecords = new List<InfoRecord>();

            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {              
                try
                {
                    await conn.OpenAsync();
                    var command = new NpgsqlCommand(query, conn);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            infoRecords.Add(new InfoRecord(reader[0].ToString()));
                        }
                        await reader.CloseAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Database does not have info table.");
                }
                finally
                {
                    await conn.CloseAsync();
                }

            }

            return infoRecords;

        }
        public async Task ExecuteCommand(string sqlCommand, string databaseName)
        {
            var connectionString = _serverConnectionString;
            if (databaseName != null) connectionString = AddDatabaseNameToServerConnectionString(databaseName);
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    var command = new NpgsqlCommand(sqlCommand, conn);
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Error executing command.");
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
        }
        public async Task<IList<IList<string>>> ExecuteQuery(string sqlQuery, string databaseName)
        {
            var records = new List<IList<string>>();
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                try
                {
                    await conn.OpenAsync();
                    var command = new NpgsqlCommand(sqlQuery, conn);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var recordValues = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                recordValues.Add(reader[i].ToString());
                            }
                            records.Add(recordValues);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Error executing query.");
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            return records;
        }

        private string AddDatabaseNameToServerConnectionString(string databaseName)
        {
            return _serverConnectionString + ";Database=" + databaseName;
        }


    }
}
