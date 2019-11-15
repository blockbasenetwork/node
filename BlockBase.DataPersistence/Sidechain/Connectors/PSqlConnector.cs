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
        //TODO: refactor this to go to configs
        private static string _ivPrefix = "iv";
        private static string _bucketPrefix = "bkt";
        private static string _joiningInfoChar = "_";

        private string _serverConnectionString;
        private ILogger _logger;

        public PSqlConnector(string serverName, string user, int port, string password, ILogger logger)
        {
            _serverConnectionString = "Server=" + serverName + ";User ID=" + user + ";Port=" + port + ";Password=" + password;
            _logger = logger;
        }



        public IList<string> GetAllTableColumns(string tableName, string databaseName)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                conn.Open();
                var sqlQuery = "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '" + tableName + "';";
                var command = new NpgsqlCommand(sqlQuery, conn);
                var reader = command.ExecuteReader();
                var columnNames = new List<string>();
                while (reader.Read())
                {
                    columnNames.Add(reader[0].ToString());
                }
                return columnNames;
            }

        }


        public IDictionary<string, string> GetAllTableColumnsAndDataTypes(string tableName, string databaseName)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(AddDatabaseNameToServerConnectionString(databaseName)))
            {
                conn.Open();
                var sqlQuery = @"SELECT c.column_name, c.data_type FROM pg_catalog.pg_statio_all_tables as st 
                    inner join pg_catalog.pg_description pgd on(pgd.objoid = st.relid)
                    right outer join information_schema.columns c on(pgd.objsubid = c.ordinal_position and c.table_schema = st.schemaname and c.table_name = st.relname)
                    where table_schema = 'public' and table_name = '" + tableName + "';";


                var command = new NpgsqlCommand(sqlQuery, conn);
                var reader = command.ExecuteReader();
                var columnNamesAndDataTypes = new Dictionary<string, string>();
                while (reader.Read())
                {
                    columnNamesAndDataTypes.Add(reader[0].ToString(), reader[1].ToString());
                }
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
