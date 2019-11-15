using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Database.Constants;
using BlockBase.Domain.Database.Operations;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using BlockBase.Domain.Database;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Database.Columns;
using BlockBase.Domain.Database.QueryResults;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public class MySqlConnector : IConnector
    {
        private string _serverConnection;
        private string _serverName;
        private string _user;
        private int _port;
        private string _password;
        private DatabaseConstants _constants;
        private ILogger _logger;
        public MySqlConnector(string serverName, string user, int port, string password, ILogger logger)
        {
            _constants = new MySqlConstants();
            _serverConnection = "server=" + serverName + ";user=" + user + ";port=" + port + ";password=" + password;
            _serverName = serverName;
            _user = user;
            _port = port;
            _password = password;
            _logger = logger;
        }
        private string CreateDBConnectionString(string databaseName)
        {
            return "server=" + _serverName + ";user=" + _user + ";database=" + databaseName.ToLower() + ";port=" + _port + ";password=" + _password;
        }
        public async Task CreateMetaInfo(string databaseName)
        {
            _logger.LogDebug("Creating meta info for database: " + databaseName);
            var databaseConnection = CreateDBConnectionString(databaseName);
            var list = new List<string>()
            {
                "CREATE TABLE " + _constants.METAINFO_TABLE_NAME + " (" + _constants.ID + " int PRIMARY KEY," + _constants.LAST_BLOCK + " int);",
                "INSERT INTO " + _constants.METAINFO_TABLE_NAME + " values (1, 0);",
                "CREATE TABLE " + _constants.COLUMN_TYPE_TABLE_NAME + " (" + _constants.ID + " int PRIMARY KEY, " + _constants.TYPE_COLUMN_NAME + " Varchar(50));",
                "INSERT INTO " + _constants.COLUMN_TYPE_TABLE_NAME + "(" + _constants.ID + "," + _constants.TYPE_COLUMN_NAME + ") values (" + _constants.PRIMARY_COLUMNS_TABLE_NAME_ID + ", '" + _constants.PRIMARY_COLUMNS_TABLE_NAME + "')",
                "INSERT INTO " + _constants.COLUMN_TYPE_TABLE_NAME + "(" + _constants.ID + "," + _constants.TYPE_COLUMN_NAME + ") values (" + _constants.FOREIGN_COLUMNS_TABLE_NAME_ID + ", '" + _constants.FOREIGN_COLUMNS_TABLE_NAME + "')",
                "INSERT INTO " + _constants.COLUMN_TYPE_TABLE_NAME + "(" + _constants.ID + "," + _constants.TYPE_COLUMN_NAME + ") values (" + _constants.UNIQUE_COLUMNS_TABLE_NAME_ID + ", '" + _constants.UNIQUE_COLUMNS_TABLE_NAME + "')",
                "INSERT INTO " + _constants.COLUMN_TYPE_TABLE_NAME + "(" + _constants.ID + "," + _constants.TYPE_COLUMN_NAME + ") values (" + _constants.RANGE_COLUMNS_TABLE_NAME_ID + ", '" + _constants.RANGE_COLUMNS_TABLE_NAME + "')",
                "INSERT INTO " + _constants.COLUMN_TYPE_TABLE_NAME + "(" + _constants.ID + "," + _constants.TYPE_COLUMN_NAME + ") values (" + _constants.NORMAL_COLUMNS_TABLE_NAME_ID + ", '" + _constants.NORMAL_COLUMNS_TABLE_NAME + "')",
                "CREATE TABLE " + _constants.COLUMN_INFO_TABLE + "( " + _constants.ID + " varchar(38) primary key, " + _constants.COLUMN_TYPE_ID + " int references " + _constants.COLUMN_TYPE_TABLE_NAME + "(" + _constants.ID + "),  " + _constants.COLUMN_INFO_TABLE_NAMES + " varchar(200) NOT NULL, " + _constants.COLUMN_NAME + " varchar(200) NOT NULL, " +
                    "constraint Column_AltPK unique (" + _constants.ID + "," + _constants.COLUMN_TYPE_ID + "))",
                "CREATE TABLE " + _constants.FOREIGN_COLUMNS_TABLE_NAME + "( " + _constants.COLUMN_ID_NAME + " varchar(38) primary key, " + _constants.COLUMN_TYPE_ID + " int DEFAULT " + _constants.FOREIGN_COLUMNS_TABLE_NAME_ID + ", " +
                    "foreign key(" + _constants.COLUMN_ID_NAME + ", " + _constants.COLUMN_TYPE_ID + ") references " + _constants.COLUMN_INFO_TABLE + "(" + _constants.ID + ", " + _constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE " + _constants.NORMAL_COLUMNS_TABLE_NAME + "( " + _constants.COLUMN_ID_NAME + " varchar(38) primary key, " + _constants.COLUMN_TYPE_ID + " int DEFAULT " + _constants.NORMAL_COLUMNS_TABLE_NAME_ID + ", " +_constants.BUCKET_SIZE_COLUMN_NAME +" varbinary(500) NOT NULL, " + _constants.BUCKET_SIZE_IV_COLUMN_NAME + " varbinary(500) NOT NULL, " +
                    "foreign key(" + _constants.COLUMN_ID_NAME + ", " + _constants.COLUMN_TYPE_ID + ") references " + _constants.COLUMN_INFO_TABLE + "(" + _constants.ID +", " + _constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE " + _constants.PRIMARY_COLUMNS_TABLE_NAME + "( " + _constants.COLUMN_ID_NAME+" varchar(38) primary key, " + _constants.COLUMN_TYPE_ID + " int DEFAULT "+_constants.PRIMARY_COLUMNS_TABLE_NAME_ID+", " +
                    "foreign key(" + _constants.COLUMN_ID_NAME + ", " + _constants.COLUMN_TYPE_ID + ") references " + _constants.COLUMN_INFO_TABLE + "(" + _constants.ID + ", " + _constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE " + _constants.RANGE_COLUMNS_TABLE_NAME + "( " + _constants.COLUMN_ID_NAME+" varchar(38) primary key, " + _constants.COLUMN_TYPE_ID + " int DEFAULT "+ _constants.RANGE_COLUMNS_TABLE_NAME_ID + ", " + _constants.BUCKET_SIZE_COLUMN_NAME + " varbinary(500) NOT NULL, " + _constants.BUCKET_SIZE_IV_COLUMN_NAME + " varbinary(500) NOT NULL, " + _constants.MAXRANGE_COLUMN + " varbinary(500) NOT NULL, " + _constants.MAX_RANGE_IV_COLUMN + " varbinary(500) NOT NULL, " + _constants.CANBENEGATIVE_COLUMN + " bit NOT NULL, " +
                     "foreign key(" + _constants.COLUMN_ID_NAME + ", " + _constants.COLUMN_TYPE_ID + ") references " + _constants.COLUMN_INFO_TABLE + "(" + _constants.ID + ", " + _constants.COLUMN_TYPE_ID + "))",
                "CREATE TABLE " + _constants.UNIQUE_COLUMNS_TABLE_NAME + "( " + _constants.COLUMN_ID_NAME + " varchar(38) primary key, " + _constants.COLUMN_TYPE_ID + " int DEFAULT " + _constants.UNIQUE_COLUMNS_TABLE_NAME_ID + "," +
                    "foreign key(" + _constants.COLUMN_ID_NAME + ", " + _constants.COLUMN_TYPE_ID + ") references " + _constants.COLUMN_INFO_TABLE + "(" + _constants.ID + ", " + _constants.COLUMN_TYPE_ID + "))"

            };
            using (var connection = new MySqlConnection(databaseConnection))
            {
                await connection.OpenAsync();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    foreach (var query in list)
                    {
                        await ExecuteCommand(query, connection, transaction);
                    }
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }

        }
        public bool CheckIfMetaInfoTableExist(string databaseName)
        {
            _logger.LogDebug("Checking if meta info exists in database: " + databaseName);
            var databaseConnection =  CreateDBConnectionString(databaseName);

            using (var connection = new MySqlConnection(databaseConnection))
            {
                connection.Open();
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {

                    using (MySqlCommand cmd = new MySqlCommand("SHOW TABLES LIKE '" + _constants.METAINFO_TABLE_NAME + "';"))
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = transaction;
                        var table = (string)cmd.ExecuteScalar();
                        transaction.Commit();
                        return (table == _constants.METAINFO_TABLE_NAME);
                    }
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }
        }
        public async Task Execute(Dictionary<string, LinkedList<ISqlOperation>> commandsToExecute)
        {
            foreach (var keyValuePair in commandsToExecute)
            {
                var databaseName = keyValuePair.Key;
                var commandsToExecutePerDatabase = keyValuePair.Value;
                var isProxyDatabase = databaseName.ToLower().Split("_")[0] == "proxy";

                var dbExists = await CheckDatabase(databaseName);
                if (!dbExists)
                {
                    using (MySqlConnection connection = new MySqlConnection(_serverConnection))
                    {
                        await connection.OpenAsync();

                        foreach (var sql in commandsToExecutePerDatabase)
                        {
                            if (sql is CreateDatabaseOperation)
                            {
                                //TODO: if the query is sent with a different database name from the transaction, 
                                //this ignores the query database name
                                await CreateDatabase(databaseName);
                                if(isProxyDatabase) await CreateMetaInfo(databaseName);
                                commandsToExecutePerDatabase.RemoveFirst();
                                dbExists = true;
                                break;
                            }
                            commandsToExecutePerDatabase.RemoveFirst();
                        }
                    }
                }

                if (dbExists == true && isProxyDatabase && !CheckIfMetaInfoTableExist(databaseName)) await CreateMetaInfo(databaseName);

                if (commandsToExecutePerDatabase.Count == 0) continue;

                var databaseConnection =  CreateDBConnectionString(databaseName);
                using (MySqlConnection connection = new MySqlConnection(databaseConnection))
                {
                    await connection.OpenAsync();
                    MySqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        foreach (var sql in commandsToExecutePerDatabase)
                        {
                            await ExecuteCommand(sql.GetMySQLQuery(), connection, transaction);
                        }
                        commandsToExecutePerDatabase.Clear();
                        transaction.Commit();

                    }
                    catch (Exception exception)
                    {
                        transaction.Rollback();
                        throw exception;
                    }
                }
            }
        }
        private async Task ExecuteCommand(string operationStr, MySqlConnection connection, MySqlTransaction transaction = null)
        {
            using (var cmd = new MySqlCommand(operationStr))
            {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                await cmd.ExecuteNonQueryAsync();
            }
        }
        public async Task<bool> CheckDatabase(string databaseName)
        {
            var lowercaseDatabaseName = databaseName.ToLower();
            var cmdText = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = ?database";
            using (var connection = new MySqlConnection(_serverConnection))
            {
                using (var sqlCmd = new MySqlCommand(cmdText, connection))
                {
                    sqlCmd.Parameters.AddWithValue("?database", lowercaseDatabaseName);
                    await connection.OpenAsync();
                    var result = (string)await sqlCmd.ExecuteScalarAsync();
                    return result == lowercaseDatabaseName;
                }
            }
        }
        public async Task CreateDatabase(string databaseName)
        {
            var cmdCreate = "CREATE DATABASE " + databaseName.ToLower();

            using (var sqlConnection = new MySqlConnection(_serverConnection))
            {
                using (var sqlCmd = new MySqlCommand(cmdCreate, sqlConnection))
                {
                    await sqlConnection.OpenAsync();
                    sqlCmd.ExecuteNonQuery();
                }
            }
        }


        //TODO: PROXY STUFF
        public DatabaseConstants GetDatabaseConstants()
        {
            return _constants;
        }
        public string GetTableNameWithPrefix(string encryptedName)
        {
            return _constants.TABLE_NAME_PREFIX + encryptedName;
        }
        public string GetColumnNameWithPrefix(string columnName)
        {
            return _constants.MAIN_ONION_PREFIX + columnName;
        }
        public string GetBucketColumnNameWithPrefix(string columnName)
        {
            return _constants.BUCKET_COLUMN_PREFIX + columnName;
        }
        public string GetIVColumnNameWithPrefix(string columnName)
        {
            return _constants.IV_NAME_PREFIX + columnName;
        }
        public List<string> QueryDBGetString(QueryBuilder query, string databaseName)
        {
            var databaseConnection = CreateDBConnectionString(databaseName);
            Transaction transaction = new Transaction();
            return transaction.ExecuteOperation(() =>
            {
                using (MySqlConnection connection = new MySqlConnection(databaseConnection))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(query.Build()))
                    {
                        cmd.Connection = connection;
                        var reader = cmd.ExecuteReader();
                        var list = new List<string>();
                        while (reader.Read())
                        {
                            list.Add(reader.GetString(0));
                        }
                        return list;
                    }
                }
            });
        }
        public IList<Tuple<string, byte[], byte[]>> GetIdentifiersWithBucket(string primaryKeyColumnName, string tableName, string valueColumn, string columnName, byte[] bucket, string databaseName)
        {
            var databaseConnection =  CreateDBConnectionString(databaseName);
            var lowerCaseTableName = tableName;
            var queryBuilder = new QueryBuilder();
            var query = queryBuilder.Select(primaryKeyColumnName, lowerCaseTableName).Select(valueColumn, lowerCaseTableName).Select(GetIVColumnNameWithPrefix(columnName), lowerCaseTableName).Where(lowerCaseTableName, GetBucketColumnNameWithPrefix(columnName), bucket);

            Transaction transaction = new Transaction();
            return transaction.ExecuteOperation(() =>
            {
                using (MySqlConnection connection = new MySqlConnection(databaseConnection))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(query.Build()))
                    {
                        cmd.Connection = connection;
                        var reader = cmd.ExecuteReader();
                        var list = new List<Tuple<string, byte[], byte[]>>();
                        while (reader.Read())
                        {
                            list.Add(new Tuple<string, byte[], byte[]>(reader.GetGuid(0).ToString(), (byte[])reader.GetValue(1), (byte[])reader.GetValue(2)));
                        }
                        return list;
                    }
                }
            });
        }
        public int QueryDBGetValues(string query, List<IResult> values, string databaseName)
        {
            var databaseConnection =  CreateDBConnectionString(databaseName);

            Transaction transaction = new Transaction();
            try
            {
                int result = transaction.ExecuteOperation(() =>
                {
                    using (MySqlConnection connection = new MySqlConnection(databaseConnection))
                    {
                        connection.Open();

                        using (var cmd = new MySqlCommand(query))
                        {
                            cmd.Connection = connection;
                            var reader = cmd.ExecuteReader();
                            int numberOfRows = 0;
                            while (reader.Read())
                            {
                                for (int i = 0; i < values.Count; i++)
                                {
                                    values[i].AddMySqlValue(reader.GetValue(i));
                                }
                                numberOfRows++;
                            }
                            return numberOfRows;
                        }
                    }
                });
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }
        public Tuple<ColumnType, string> GetColumnTypeAndId(string encryptedValueColumn, string encryptedTableName, string databaseName)
        {
            var queryBuilder = new QueryBuilder();
            var query = queryBuilder.Select(_constants.COLUMN_TYPE_ID, _constants.COLUMN_INFO_TABLE).Select(_constants.ID, _constants.COLUMN_INFO_TABLE).Where(_constants.COLUMN_INFO_TABLE, _constants.COLUMN_NAME, encryptedValueColumn).Where(_constants.COLUMN_INFO_TABLE, _constants.COLUMN_INFO_TABLE_NAMES, encryptedTableName).Build();
            var listResult = new List<IResult>
            {
                new IntQueryResult() { TableName = encryptedTableName },
                new GuidQueryResult() { TableName = encryptedTableName }
            };
            QueryDBGetValues(query, listResult, databaseName);
            var intResult = (IntQueryResult)listResult[0];
            var guidResult = (GuidQueryResult)listResult[1];
            
            if (intResult.Values.Count != 1)
            {
                throw new Exception("Column or table doesnt exist.");
            }
            return new Tuple<ColumnType, string>((ColumnType)intResult.Values[0], guidResult.Values[0]);
        }
        public Tuple<byte[], byte[]> GetBucket(string guidString, ColumnType type, string databaseName)
        {
            var queryBuilder = new QueryBuilder();
            var guid = Guid.Parse(guidString);
            string query;
            if (type == ColumnType.RangeColumn)
            {
                query = "SELECT BucketSize, BucketSizeIV FROM rangecolumns WHERE ColumnId = '" + guidString + "';";
            }
            else
            {
                query = "SELECT BucketSize, BucketSizeIV FROM normalcolumns WHERE ColumnId = '" + guidString + "';";
            }
            var listResult = new List<IResult>
            {
                new BinaryQueryResult(),
                new BinaryQueryResult()
            };
            QueryDBGetValues(query, listResult, databaseName);
            var bucketSizeResult = (BinaryQueryResult)listResult[0];
            var bucketSizeResultIV = (BinaryQueryResult)listResult[1];
            return new Tuple<byte[], byte[]>(bucketSizeResult.Values[0], bucketSizeResultIV.Values[0]);

        }
        public List<IResult> GetAllBucketSize(string encryptedTableName, string databaseName)
        {
            var queryBuilder = new QueryBuilder();

            string query = "Select columninfo.ColumnName,columninfo.ColumnTypeId,normalcolumns.BucketSize, normalcolumns.BucketSizeIV, rangecolumns.BucketSize, rangecolumns.BucketSizeIV ,rangecolumns.MaxRange, rangecolumns.MaxRangeIV FROM columninfo LEFT JOIN normalcolumns ON columninfo.Id = normalcolumns.ColumnId LEFT JOIN rangecolumns on columninfo.Id = rangecolumns.ColumnId where columninfo.TableName = '" + encryptedTableName + "';";
            var listResult = new List<IResult>
            {
                new StringQueryResult() { TableName = encryptedTableName },
                new IntQueryResult() { TableName = encryptedTableName },
                new BinaryQueryResult() { TableName = encryptedTableName },
                new BinaryQueryResult() { TableName = encryptedTableName },
                new BinaryQueryResult() { TableName = encryptedTableName },
                new BinaryQueryResult() { TableName = encryptedTableName },
                new BinaryQueryResult() { TableName = encryptedTableName },
                new BinaryQueryResult() { TableName = encryptedTableName },

            };
            QueryDBGetValues(query, listResult, databaseName);
            return listResult;
        }
        public Tuple<byte[], byte[], bool> GetBucketMaxRange(string guidString, string databaseName)
        {
            var guid = Guid.Parse(guidString);
            var query = "SELECT MaxRange, MaxRangeIV, CanBeNegative FROM rangecolumns WHERE ColumnId = '" + guidString + "';";
            var listResult = new List<IResult>
            {
                new BinaryQueryResult(),
                new BinaryQueryResult(),
                new BooleanQueryResult()
            };
            QueryDBGetValues(query, listResult, databaseName);
            var maxRangeResult = (BinaryQueryResult)listResult[0];
            var maxRangeIVResult = (BinaryQueryResult)listResult[1];
            var canBeNegativeResult = (BooleanQueryResult)listResult[2];
            return new Tuple<byte[], byte[], bool>(maxRangeResult.Values[0], maxRangeIVResult.Values[0], canBeNegativeResult.Values[0]);
        }
        public string GetPrimaryKey(string tableName, string databaseName)
        {
            var queryBuilder = new QueryBuilder();
            var query = queryBuilder.Select(_constants.COLUMN_NAME, _constants.COLUMN_INFO_TABLE).Where(_constants.COLUMN_INFO_TABLE, _constants.COLUMN_TYPE_ID, 1).Where(_constants.COLUMN_INFO_TABLE, _constants.COLUMN_INFO_TABLE_NAMES, tableName);
            return QueryDBGetString(query, databaseName)[0];
        }
        public Dictionary<string, List<Tuple<string, string>>> GetStructure(string databaseName)
        {
            var query = "SELECT columninfo.TableName, columninfo.ColumnName, columntype.Type from columninfo inner join columntype on columninfo.ColumnTypeId = columntype.Id";
            var listResult = new List<IResult>
            {
                new StringQueryResult(),
                new StringQueryResult(),
                new StringQueryResult()
            };
            QueryDBGetValues(query, listResult, databaseName);
            var tableNames = (StringQueryResult)listResult[0];
            var columnNames = (StringQueryResult)listResult[1];
            var types = (StringQueryResult)listResult[2];
            int resultRows = tableNames.Values.Count;
            if (resultRows == 0)
            {
                return null;
            }
            else
            {
                var info = new Dictionary<string, List<Tuple<string, string>>>();
                for (int i = 0; i < resultRows; i++)
                {
                    var Columns = info.GetValueOrDefault(tableNames.Values[i]);
                    if (Columns == null)
                    {
                        var list = new List<Tuple<string, string>>
                        {
                            new Tuple<string, string>(columnNames.Values[i], types.Values[i])
                        };
                        info.Add(tableNames.Values[i], list);
                    }
                    else
                    {
                        Columns.Add(new Tuple<string, string>(columnNames.Values[i], types.Values[i]));
                    }
                }
                return info;
            }
        }
    }

}

