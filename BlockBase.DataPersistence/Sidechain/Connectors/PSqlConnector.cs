using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Database.Info;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public class PSqlConnector : IConnector
    {
        private string _serverConnectionString;
        private ILogger _logger;
        private static readonly string DEFAULT_DATABASE_NAME = "blockbasedb";
        private static readonly string DATABASES_TABLE_NAME = "databases";
        private static readonly string INFO_TABLE_NAME = "info";
        private bool _hasBeenSetup = false;
        private string _defaultDatabaseNameWithPrefix;


        public PSqlConnector(IOptions<NodeConfigurations> nodeConfigurations, ILogger<PSqlConnector> logger)
        {
            var nodeConfigurationsValue = nodeConfigurations.Value;
            _serverConnectionString = "Server=" + nodeConfigurationsValue.PostgresHost
            + ";User ID=" + nodeConfigurationsValue.PostgresUser
            + ";Port=" + nodeConfigurationsValue.PostgresPort
            + ";Password=" + nodeConfigurationsValue.PostgresPassword;
            _logger = logger;
            _defaultDatabaseNameWithPrefix = nodeConfigurationsValue.DatabasesPrefix + DEFAULT_DATABASE_NAME;

        }

        public async Task<bool> TestConnection() 
        {
            using(NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                try
                {
                    await conn.OpenAsync(); 
                    return conn.State == System.Data.ConnectionState.Open;
                    
                }
                catch(Exception e)
                {
                    _logger.LogDebug(e.Message, "Unable to connect to postgres");
                    return false;
                }
            }
        }

        public async Task Setup()
        {
            if(!_hasBeenSetup)
            {
                //TODO rpinto - what if this fails? There's no info returning from this method
                await CreateDefaultDatabaseIfNotExists();
                _hasBeenSetup = true;
            }
        }

        public async Task<IList<InfoRecord>> GetInfoRecords()
        {
            var infoRecords = new List<InfoRecord>();
            var databases = await GetDatabaseList();
            foreach (var databaseName in databases)
            {
                infoRecords.AddRange(await GetInfoRecordsFromDatabase(databaseName));
            }
            return infoRecords;
        }

        public async Task InsertToDatabasesTable(string databaseName)
        {
            await ExecuteCommand($"INSERT INTO {DATABASES_TABLE_NAME} (name) VALUES ( '{databaseName}' );", _defaultDatabaseNameWithPrefix);
        }

        public async Task DeleteFromDatabasesTable(string databaseName)
        {
            await ExecuteCommand($"DELETE FROM {DATABASES_TABLE_NAME} WHERE name = '{databaseName}';", _defaultDatabaseNameWithPrefix);
        }
        private async Task<IList<InfoRecord>> GetInfoRecordsFromDatabase(string databaseName)
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

        private async Task CreateDefaultDatabaseIfNotExists()
        {
            
            bool dbExists = false;
            using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                await conn.OpenAsync();
                string cmdText = $"SELECT 1 FROM pg_database WHERE datname='{_defaultDatabaseNameWithPrefix}'";
                NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
                try
                {
                    dbExists = await cmd.ExecuteScalarAsync() != null;
                    if (!dbExists)
                    {
                        cmd.Dispose();
                        cmd = new NpgsqlCommand($"CREATE DATABASE {_defaultDatabaseNameWithPrefix};", conn);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Unable to create default database");
                }
                finally
                {
                    cmd.Dispose();
                    await conn.CloseAsync();
                }
            }
            await CreateTableIfNotExists();
            
        }
        public async Task DropDefaultDatabase()
        {
          await DropDatabase(_defaultDatabaseNameWithPrefix);  
        }

        public async Task DropDatabase(string databaseName)
        {
             using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                await conn.OpenAsync();
                NpgsqlCommand cmd = new NpgsqlCommand($"DROP DATABASE {databaseName};", conn);
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                    _hasBeenSetup = false;
                }
                catch (Exception)
                {
                }
                finally
                {
                    cmd.Dispose();
                    await conn.CloseAsync();
                }
            }
        }


        private async Task CreateTableIfNotExists()
        {
            bool tableExists = false;
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(_defaultDatabaseNameWithPrefix)))
            {
                await conn.OpenAsync();
                string cmdText = $"select * from information_schema.tables where table_name ='{DATABASES_TABLE_NAME}';";
                NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
                try
                {
                    tableExists = await cmd.ExecuteScalarAsync() != null;
                    if (!tableExists)
                    {
                        cmd.Dispose();
                        cmd = new NpgsqlCommand($"CREATE TABLE {DATABASES_TABLE_NAME} ( name TEXT PRIMARY KEY );", conn);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Unable to create default table");
                }
                finally
                {
                    cmd.Dispose();
                    await conn.CloseAsync();
                }
            }
        }
        private async Task<IList<string>> GetDatabaseList()
        {
            var query = $"SELECT * from {DATABASES_TABLE_NAME};";

            var results = new List<string>();

            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(_defaultDatabaseNameWithPrefix)))
            {
                try
                {
                    await conn.OpenAsync();
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(reader[0].ToString());
                            }
                            await reader.CloseAsync();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Default database does not exist.");
                }
                finally
                {
                    await conn.CloseAsync();
                }

            }
            return results;
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
                    using (var command = new NpgsqlCommand(sqlCommand, conn))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Error executing command.");
                    throw e;
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
                    using (var command = new NpgsqlCommand(sqlQuery, conn))
                    {
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
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Error executing query.");
                    throw e;
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
            return _serverConnectionString + ";Database=" + databaseName + ";Pooling=false";
        }


    }
}
