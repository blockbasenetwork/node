using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public class PSqlConnector
    {
        private string _serverConnectionString;
        private ILogger _logger;

        private static readonly string COLUMN_INFO_TABLE_NAME = "column_info";
        private static readonly string COLUMN_INFO_COLUMN_NAME = "column_name";
        private static readonly string COLUMN_INFO_NAME_ENCRYPTED = "name_encrypted";
        private static readonly string COLUMN_INFO_DATA_ENCRYPTED = "data_encrypted";

        public PSqlConnector(string serverName, string user, int port, string password, ILogger logger)
        {
            _serverConnectionString = "Server=" + serverName + ";User ID=" + user + ";Port=" + port + ";Password=" + password;
            _logger = logger;
        }



        public IDictionary<string, bool> GetAllTableColumnsAndNameEncryptedIndicator(string tableName, string databaseName)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                conn.Open();
                var sqlQuery = "SELECT information_schema.columns.column_name, " + COLUMN_INFO_NAME_ENCRYPTED 
                    + " FROM information_schema.columns LEFT JOIN " + COLUMN_INFO_TABLE_NAME + " ON information_schema.columns.column_name = "  + COLUMN_INFO_TABLE_NAME + "." + COLUMN_INFO_COLUMN_NAME +
                    " WHERE table_schema = 'public' AND table_name = '" + tableName + "';";

                var command = new NpgsqlCommand(sqlQuery, conn);
                var reader = command.ExecuteReader();

                var columnNamesAndNameEncryptedIndicator = new Dictionary<string, bool>();
                while (reader.Read())
                {
                    columnNamesAndNameEncryptedIndicator.Add(reader[0].ToString(), bool.TryParse(reader[1].ToString(), out bool result) ? result : false);
                }
                return columnNamesAndNameEncryptedIndicator;
            }
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
                var reader = command.ExecuteReader();

                var columnNamesAndDataTypes = new Dictionary<string, string>();

                while (reader.Read())
                {
                    columnNamesAndDataTypes.Add(reader[0].ToString(), reader[1].ToString());
                }
                reader.Close();
                
                sqlQuery = @"DROP VIEW column_data_type_" + tableName + ";";
                command = new NpgsqlCommand(sqlQuery, conn);
                command.ExecuteNonQuery();

                return columnNamesAndDataTypes;
            }

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

        private string AddDatabaseNameToServerConnectionString(string databaseName)
        {
            return _serverConnectionString + ";Database=" + databaseName;
        }
    }
}
