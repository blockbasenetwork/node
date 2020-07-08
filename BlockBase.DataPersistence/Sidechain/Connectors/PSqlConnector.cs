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
        private static readonly string TRANSACTION_INFO_TABLE_NAME = "blockbase_transaction_info";
        private static readonly string SEQUENCE_NUMBER_COLUMN_NAME = "sequence_number";
        private static readonly string INFO_TABLE_NAME = "info";
        private bool _hasBeenSetup = false;
        private string _dbPrefix;


        public PSqlConnector(IOptions<NodeConfigurations> nodeConfigurations, ILogger<PSqlConnector> logger)
        {
            var nodeConfigurationsValue = nodeConfigurations.Value;
            _serverConnectionString = "Server=" + nodeConfigurationsValue.PostgresHost
            + ";User ID=" + nodeConfigurationsValue.PostgresUser
            + ";Port=" + nodeConfigurationsValue.PostgresPort
            + ";Password=" + nodeConfigurationsValue.PostgresPassword;
            _logger = logger;
            _dbPrefix = nodeConfigurationsValue.DatabasesPrefix;

        }

        public async Task<bool> TestConnection()
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    return conn.State == System.Data.ConnectionState.Open;

                }
                catch (Exception e)
                {
                    _logger.LogDebug(e.Message, "Unable to connect to postgres");
                    return false;
                }
            }
        }

        public async Task Setup()
        {
            if (!_hasBeenSetup)
            {
                //TODO rpinto - what if this fails? There's no info returning from this method
                await CreateDefaultDatabase();
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
            await ExecuteCommand($"INSERT INTO {DATABASES_TABLE_NAME} (name) VALUES ( '{databaseName}' );", DEFAULT_DATABASE_NAME);
        }

        public async Task DeleteFromDatabasesTable(string databaseName)
        {
            await ExecuteCommand($"DELETE FROM {DATABASES_TABLE_NAME} WHERE name = '{databaseName}';", DEFAULT_DATABASE_NAME);
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

        public async Task<bool> DoesDatabaseExist(string databaseName)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                await conn.OpenAsync();
                string cmdText = $"SELECT 1 FROM pg_database WHERE datname='{_dbPrefix + databaseName}'";
                NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);

                bool databaseExists = await cmd.ExecuteScalarAsync() != null;

                cmd.Dispose();
                await conn.CloseAsync();

                return databaseExists;

            }
        }
        public async Task<bool> DoesDefaultDatabaseExist()
        {
            return await DoesDatabaseExist(DEFAULT_DATABASE_NAME);
        }
        private async Task CreateDefaultDatabase()
        {
            bool dbExists = await DoesDefaultDatabaseExist();
            using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                await conn.OpenAsync();
                NpgsqlCommand cmd = new NpgsqlCommand($"CREATE DATABASE {_dbPrefix + DEFAULT_DATABASE_NAME};", conn);
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e.Message, "Unable to create default database.");
                }
                finally
                {
                    cmd.Dispose();
                    await conn.CloseAsync();
                }
            }

            await CreateDatabasesTableIfNotExists();

        }
        public async Task DropDefaultDatabase()
        {
            await DropDatabase(DEFAULT_DATABASE_NAME);
        }

        public async Task DropDatabase(string databaseName)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                await conn.OpenAsync();
                NpgsqlCommand cmd = new NpgsqlCommand($"DROP DATABASE {_dbPrefix + databaseName};", conn);
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


        private async Task CreateDatabasesTableIfNotExists()
        {
            bool tableExists = false;
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(DEFAULT_DATABASE_NAME)))
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

        private async Task CreateTransactionInfoTableIfNotExists(string databaseName)
        {
            bool tableExists = false;
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                await conn.OpenAsync();
                string cmdText = $"select * from information_schema.tables where table_name ='{TRANSACTION_INFO_TABLE_NAME}';";
                NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
                try
                {
                    tableExists = await cmd.ExecuteScalarAsync() != null;
                    if (!tableExists)
                    {
                        cmd.Dispose();
                        cmd = new NpgsqlCommand($"CREATE TABLE {TRANSACTION_INFO_TABLE_NAME} ( {SEQUENCE_NUMBER_COLUMN_NAME} NUMERIC PRIMARY KEY );", conn);
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

            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(DEFAULT_DATABASE_NAME)))
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
            if (databaseName != "") connectionString = AddDatabaseNameToServerConnectionString(databaseName);
            else 
            {
                var index = sqlCommand.IndexOf("_");
                sqlCommand = sqlCommand.Insert(index, _dbPrefix);
            }
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

        public async Task ExecuteCommandWithTransactionNumber(string sqlCommand, string databaseName, ulong transactionNumer)
        {
            var connectionString = _serverConnectionString;

            connectionString = AddDatabaseNameToServerConnectionString(databaseName);
            await CreateTransactionInfoTableIfNotExists(databaseName);

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                    var transaction = conn.BeginTransaction();
                    using (var command1 = new NpgsqlCommand(sqlCommand, conn))
                    {
                        await command1.ExecuteNonQueryAsync();
                    }

                    using (var command2 = new NpgsqlCommand($"INSERT INTO {TRANSACTION_INFO_TABLE_NAME} ({SEQUENCE_NUMBER_COLUMN_NAME}) VALUES ( {transactionNumer} );", conn))
                    {
                        await command2.ExecuteNonQueryAsync();
                    }
                    await transaction.CommitAsync();

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

        public async Task<bool> WasTransactionExecuted(string databaseName, ulong transactionNumer)
        {
            try
            {
                var query = $"SELECT * FROM {TRANSACTION_INFO_TABLE_NAME} WHERE {SEQUENCE_NUMBER_COLUMN_NAME} = {transactionNumer}";
                var results = await ExecuteQuery(query, databaseName);
                return results.Count != 0;
            }
            catch
            {
                return false;
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
            return _serverConnectionString + ";Database=" + _dbPrefix + databaseName + ";Pooling=false";
        }


    }
}
