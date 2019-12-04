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
        private static readonly string COLUMN_INFO_TABLE_NAME = "column_info";
        private static readonly string COLUMN_INFO_COLUMN_NAME = "column_name";
        private static readonly string COLUMN_INFO_NAME_ENCRYPTED = "name_encrypted";

        private static readonly List<string> DEFAULT_DBS  = new List<string>() { "template0", "template1" };


        public PSqlConnector(string serverName, string user, int port, string password, ILogger logger)
        {
            _serverConnectionString = "Server=" + serverName + ";User ID=" + user + ";Port=" + port + ";Password=" + password;
            _logger = logger;
        }

        public IDictionary<string, string> GetAllTableColumnsAndDataTypes(string tableName, string databaseName)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                conn.Open();
                var viewName = "column_data_type_" + tableName;
                var sqlQuery = $@"CREATE VIEW {viewName} AS SELECT c.column_name, c.data_type FROM pg_catalog.pg_statio_all_tables as st 
                    inner join pg_catalog.pg_description pgd on(pgd.objoid = st.relid)
                    right outer join information_schema.columns c on(pgd.objsubid = c.ordinal_position and c.table_schema = st.schemaname and c.table_name = st.relname)
                    where table_schema = 'public' and table_name = '{tableName}';";


                var command = new NpgsqlCommand(sqlQuery, conn);
                command.ExecuteNonQuery();


                sqlQuery = $@"SELECT {viewName}.column_name,  CASE WHEN name_encrypted = TRUE THEN 'encrypted' ELSE data_type END FROM {viewName} LEFT JOIN {COLUMN_INFO_TABLE_NAME}
                        ON {viewName}.column_name = {COLUMN_INFO_TABLE_NAME}.{COLUMN_INFO_COLUMN_NAME};";

                command = new NpgsqlCommand(sqlQuery, conn);


                var columnNamesAndDataTypes = new Dictionary<string, string>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columnNamesAndDataTypes.Add(reader[0].ToString(), reader[1].ToString());
                    }
                }

                sqlQuery = $@"DROP VIEW {viewName};";
                command = new NpgsqlCommand(sqlQuery, conn);
                command.ExecuteNonQuery();

                return columnNamesAndDataTypes;
            }

        }

        public async Task<IList<string>> GetDatabasesList()
        {
            var query = @"SELECT pg_database.datname as Database
                        FROM pg_database, pg_user
                        WHERE pg_database.datdba = pg_user.usesysid";

            var databasesNames = new List<string>();

            using (NpgsqlConnection conn = new NpgsqlConnection(_serverConnectionString))
            {
                await conn.OpenAsync();
                var command = new NpgsqlCommand(query, conn);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        databasesNames.Add(reader[0].ToString());
                    }
                    await reader.CloseAsync();
                }
                await conn.CloseAsync();
            }

            foreach (var defaultDB in DEFAULT_DBS) databasesNames.Remove(defaultDB);

            return databasesNames;
        }
        public async Task<IList<InfoRecord>> GetInfoRecords(string databaseName)
        {
            var query = $"SELECT row_to_json(result) from (SELECT * FROM {INFO_TABLE_NAME}) AS result;";

            var infoRecords = new List<InfoRecord>();

            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                await conn.OpenAsync();
                try
                {
                    var command = new NpgsqlCommand(query, conn);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            infoRecords.Add(new InfoRecord(reader[0].ToString()));
                        }
                        await reader.CloseAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogInformation(e, "Database does not have info table.");
                }
                finally
                {
                    await conn.CloseAsync();
                }

            }

            return infoRecords;

        }
        public void ExecuteCommand(string sqlCommand, string databaseName)
        {
            var connectionString = _serverConnectionString;
            if (databaseName != null) connectionString = AddDatabaseNameToServerConnectionString(databaseName);
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                var command = new NpgsqlCommand(sqlCommand, conn);
                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        //TODO: Temporary, need to remove later
        public Tuple<string, string> TransformQuery(SelectCoreStatement selectCoreStatement, estring databaseName)
        {
            var builder = new Builder();
            builder.AddStatement(selectCoreStatement, databaseName);
            return new Tuple<string, string>(builder.BuildQueryStrings(new PSqlGenerator()).SingleOrDefault().Key, builder.BuildQueryStrings(new PSqlGenerator()).SingleOrDefault().Value[0].Value);

        }


        public IList<IList<string>> ExecuteQuery(string sqlQuery, string databaseName)
        {
            var records = new List<IList<string>>();
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                conn.Open();
                var command = new NpgsqlCommand(sqlQuery, conn);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
            return records;
        }

        private string AddDatabaseNameToServerConnectionString(string databaseName)
        {
            return _serverConnectionString + ";Database=" + databaseName;
        }


    }
}
