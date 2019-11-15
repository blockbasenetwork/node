using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;
using BlockBase.Domain.Database.Constants;
using BlockBase.Domain.Database.Operations;
using BlockBase.Domain.Database.QueryResults;
using BlockBase.DataPersistence.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public class SqlServerConnector : IConnector
    {
        private string _serverConnection;
        public DatabaseConstants Constants { get; }

        public string ServerName { get; set; }

        public SqlServerConnector(string serverName)
        {
            Constants = new DatabaseConstants();
            _serverConnection = @"Data Source=" + serverName + ";Integrated Security=True";
            ServerName = serverName;
        }

        public async Task CreateMetaInfo(string databaseName)
        {
            var databaseConnection = @"Data Source=" + ServerName + ";Initial Catalog=" + databaseName + ";Integrated Security=True";
            var list = new List<string>()
            {
                "CREATE TABLE " +Constants.METAINFO_TABLE_NAME +" (" +Constants.ID +" int PRIMARY KEY,"+Constants.LAST_BLOCK+" int);",
                "insert into " +Constants.METAINFO_TABLE_NAME +" values (1, 0);",
                "CREATE TABLE " +Constants.COLUMN_TYPE_TABLE_NAME +" (" +Constants.ID +" int PRIMARY KEY, " +Constants.TYPE_COLUMN_NAME +" Varchar(50));",
                "INSERT INTO " +Constants.COLUMN_TYPE_TABLE_NAME +"(" +Constants.ID +"," +Constants.TYPE_COLUMN_NAME +") values ("+Constants.PRIMARY_COLUMNS_TABLE_NAME_ID+", '"+Constants.PRIMARY_COLUMNS_TABLE_NAME+"')",
                "INSERT INTO " +Constants.COLUMN_TYPE_TABLE_NAME +"(" +Constants.ID +"," +Constants.TYPE_COLUMN_NAME +") values ("+Constants.FOREIGN_COLUMNS_TABLE_NAME_ID+", '"+Constants.FOREIGN_COLUMNS_TABLE_NAME+"')",
                "INSERT INTO " +Constants.COLUMN_TYPE_TABLE_NAME +"(" +Constants.ID +"," +Constants.TYPE_COLUMN_NAME +") values ("+Constants.UNIQUE_COLUMNS_TABLE_NAME_ID+", '"+Constants.UNIQUE_COLUMNS_TABLE_NAME+"')",
                "INSERT INTO " +Constants.COLUMN_TYPE_TABLE_NAME +"(" +Constants.ID +"," +Constants.TYPE_COLUMN_NAME +") values ("+Constants.RANGE_COLUMNS_TABLE_NAME_ID+", '"+Constants.RANGE_COLUMNS_TABLE_NAME+"')",
                "INSERT INTO " +Constants.COLUMN_TYPE_TABLE_NAME +"(" +Constants.ID +"," +Constants.TYPE_COLUMN_NAME +") values ("+Constants.NORMAL_COLUMNS_TABLE_NAME_ID+", '"+Constants.NORMAL_COLUMNS_TABLE_NAME+"')",
                "CREATE TABLE " +Constants.COLUMN_INFO_TABLE +"( " +Constants.ID +" uniqueidentifier primary key, " +Constants.COLUMN_TYPE_ID +" int references " +Constants.COLUMN_TYPE_TABLE_NAME +"(" +Constants.ID +"),  " +Constants.COLUMN_INFO_TABLE_NAMES +" varchar(200) NOT NULL, " +Constants.COLUMN_NAME +" varchar(200) NOT NULL, " +
                    "constraint Column_AltPK unique (" +Constants.ID +"," +Constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE ForeignColumns( ColumnId uniqueidentifier primary key, " +Constants.COLUMN_TYPE_ID +" as "+Constants.FOREIGN_COLUMNS_TABLE_NAME_ID+" persisted, " +
                    "foreign key(ColumnId, " +Constants.COLUMN_TYPE_ID +") references " +Constants.COLUMN_INFO_TABLE +"(" +Constants.ID +", " +Constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE NormalColumns( ColumnId uniqueidentifier primary key, " +Constants.COLUMN_TYPE_ID +" as " + Constants.NORMAL_COLUMNS_TABLE_NAME_ID + " persisted, " + Constants.BUCKET_SIZE_COLUMN_NAME +" varbinary(500) NOT NULL, " +Constants.BUCKET_SIZE_IV_COLUMN_NAME +" varbinary(500) NOT NULL, " +
                    "foreign key(ColumnId, " +Constants.COLUMN_TYPE_ID +") references " +Constants.COLUMN_INFO_TABLE +"(" +Constants.ID +", " +Constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE PrimaryColumns( ColumnId uniqueidentifier primary key, " +Constants.COLUMN_TYPE_ID +" as "+Constants.PRIMARY_COLUMNS_TABLE_NAME_ID+" persisted, " +
                    "foreign key(ColumnId, " +Constants.COLUMN_TYPE_ID +") references " +Constants.COLUMN_INFO_TABLE +"(" +Constants.ID +", " +Constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE RangeColumns( ColumnId uniqueidentifier primary key, " +Constants.COLUMN_TYPE_ID +" as "+Constants.RANGE_COLUMNS_TABLE_NAME_ID+" persisted, " + Constants.BUCKET_SIZE_COLUMN_NAME +" varbinary(500) NOT NULL, " +Constants.BUCKET_SIZE_IV_COLUMN_NAME +" varbinary(500) NOT NULL, " +Constants.MAXRANGE_COLUMN +" varbinary(500) NOT NULL, " +Constants.MAX_RANGE_IV_COLUMN +" varbinary(500) NOT NULL, " +Constants.CANBENEGATIVE_COLUMN +" bit NOT NULL, " +
                     "foreign key(ColumnId, " +Constants.COLUMN_TYPE_ID +") references " +Constants.COLUMN_INFO_TABLE +"(" +Constants.ID +", " +Constants.COLUMN_TYPE_ID +"))",
                "CREATE TABLE UniqueColumns( ColumnId uniqueidentifier primary key, " +Constants.COLUMN_TYPE_ID +" as "+Constants.UNIQUE_COLUMNS_TABLE_NAME_ID+" persisted ," +
                    "foreign key(ColumnId, " +Constants.COLUMN_TYPE_ID +") references " +Constants.COLUMN_INFO_TABLE +"(" +Constants.ID +", " +Constants.COLUMN_TYPE_ID +"))"

            };

            Transaction transaction = new Transaction();
            await transaction.ExecuteOperation(async () =>
            {
                using (SqlConnection connection = new SqlConnection(databaseConnection))
                {
                    await connection.OpenAsync();
                    foreach (var query in list)
                    {
                        using (var cmd = new SqlCommand(query))
                        {
                            cmd.Connection = connection;
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            });
        }
        public async Task Execute(Dictionary<string, LinkedList<ISqlOperation>> commandsToExecute)
        {
            foreach (var keyValuePair in commandsToExecute)
            {
                var databaseName = keyValuePair.Key;
                var commandsToExecutePerDatabase = keyValuePair.Value;

                var databaseConnection = @"Data Source=" + ServerName + ";Initial Catalog=" + databaseName + ";Integrated Security=True";

                Transaction transaction = new Transaction();
                await transaction.ExecuteOperation(async () =>
               {
                   using (SqlConnection connection = new SqlConnection(databaseConnection))
                   {
                       await connection.OpenAsync();
                       foreach (var sql in commandsToExecutePerDatabase)
                       {
                           using (var cmd = new SqlCommand(sql.GetSQLQuery()))
                           {
                               cmd.Connection = connection;
                               await cmd.ExecuteNonQueryAsync();
                           }
                       }
                       commandsToExecutePerDatabase.Clear();
                   }
               });
            }
        }

        public async Task<bool> CheckDatabase(string databaseName)
        {
            var cmdText = "SELECT name FROM master.dbo.sysdatabases where name = @database ;";
            using (var sqlConnection = new SqlConnection(_serverConnection))
            {
                using (var sqlCmd = new SqlCommand(cmdText, sqlConnection))
                {
                    sqlCmd.Parameters.AddWithValue("@database", databaseName);

                    sqlConnection.Open();
                    var result = (string) await sqlCmd.ExecuteScalarAsync();
                    return result == databaseName;
                }
            }
        }

        public async Task CreateDatabase(string databaseName)
        {
            var cmdCreate = "CREATE DATABASE " + databaseName;

            using (var sqlConnection = new SqlConnection(_serverConnection))
            {
                using (var sqlCmd = new SqlCommand(cmdCreate, sqlConnection))
                {
                    await sqlConnection.OpenAsync();
                    await sqlCmd.ExecuteNonQueryAsync();
                }
            }
        }

        public bool CheckIfMetaInfoTableExist(string databaseName)
        {
            var databaseConnection = @"Data Source=" + ServerName + ";Initial Catalog=" + databaseName + ";Integrated Security=True";

            using (var connection = new SqlConnection(databaseConnection))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {

                    using (SqlCommand cmd = new SqlCommand("SHOW TABLES LIKE '" + Constants.METAINFO_TABLE_NAME + "';"))
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = transaction;
                        var table = (string)cmd.ExecuteScalar();
                        transaction.Commit();
                        return (table == Constants.METAINFO_TABLE_NAME);


                    }

                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }
        }
        
        //PROXY STUFF
               public string GetTableNameWithPrefix(string encryptedName)
        {
            return Constants.TABLE_NAME_PREFIX + encryptedName;
        }

        public string GetColumnNameWithPrefix(string columnName)
        {
            return Constants.MAIN_ONION_PREFIX + columnName;
        }
        public string GetBucketColumnNameWithPrefix(string columnName)
        {
            return Constants.BUCKET_COLUMN_PREFIX + columnName;
        }
        public string GetIVColumnNameWithPrefix(string columnName)
        {
            return Constants.IV_NAME_PREFIX + columnName;
        }
        public List<string> QueryDBGetString(QueryBuilder query, string databaseName)
        {
            var databaseConnection = @"Data Source=" + ServerName + ";Initial Catalog=" + databaseName + ";Integrated Security=True";

            Transaction transaction = new Transaction();
            return transaction.ExecuteOperation(() =>
            {
                using (SqlConnection connection = new SqlConnection(databaseName))
                {
                    connection.Open();

                    using (var cmd = new SqlCommand(query.Build()))
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
            var databaseConnection = @"Data Source=" + ServerName + ";Initial Catalog=" + databaseName + ";Integrated Security=True";

            var queryBuilder = new QueryBuilder();
            var query = queryBuilder.Select(primaryKeyColumnName, tableName).Select(valueColumn, tableName).Select(GetIVColumnNameWithPrefix(columnName), tableName).Where(tableName, GetBucketColumnNameWithPrefix(columnName), bucket);

            Transaction transaction = new Transaction();
            return transaction.ExecuteOperation(() =>
            {
                using (SqlConnection connection = new SqlConnection(databaseConnection))
                {
                    connection.Open();

                    using (var cmd = new SqlCommand(query.Build()))
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
            var databaseConnection = @"Data Source=" + ServerName + ";Initial Catalog=" + databaseName + ";Integrated Security=True";

            Transaction transaction = new Transaction();
            return transaction.ExecuteOperation(() =>
            {
                using (SqlConnection connection = new SqlConnection(databaseConnection))
                {
                    connection.Open();

                    using (var cmd = new SqlCommand(query))
                    {
                        cmd.Connection = connection;
                        var reader = cmd.ExecuteReader();
                        int count = 0;
                        while (reader.Read())
                        {
                            for (int i = 0; i < values.Count; i++)
                            {
                                values[i].AddSqlValue(reader.GetValue(i));
                            }
                            count++;
                        }
                        return count;
                    }
                }
            });
        }
        public Tuple<ColumnType, string> GetColumnTypeAndId(string encryptedValueColumn, string encryptedTableName, string databaseName)
        {
            var queryBuilder = new QueryBuilder();
            var query = queryBuilder.Select("ColumnTypeId", "ColumnInfo").Select("Id", "ColumnInfo").Where("ColumnInfo", Constants.COLUMN_NAME, encryptedValueColumn).Where("ColumnInfo", "TableName", encryptedTableName).Build();
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
                query = "SELECT BucketSize, BucketSizeIV FROM RangeColumns WHERE ColumnId = '" + guidString + "';";

            }
            else
            {
                query = "SELECT BucketSize, BucketSizeIV FROM NormalColumns WHERE ColumnId = '" + guidString + "';";
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

            string query = "Select ColumnInfo.ColumnName,ColumnInfo.ColumnTypeId,NormalColumns.BucketSize, NormalColumns.BucketSizeIV, RangeColumns.BucketSize, RangeColumns.BucketSizeIV ,RangeColumns.MaxRange, RangeColumns.MaxRangeIV FROM ColumnInfo LEFT JOIN NormalColumns ON ColumnInfo.Id = NormalColumns.ColumnId LEFT JOIN RangeColumns on ColumnInfo.Id = RangeColumns.ColumnId where ColumnInfo.TableName = '" + encryptedTableName + "';";
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
            var queryBuilder = new QueryBuilder();
            var guid = Guid.Parse(guidString);
            var query = "SELECT MaxRange, MaxRangeIV, CanBeNegative FROM RangeColumns WHERE ColumnId = '" + guidString + "';";
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
            var query = queryBuilder.Select(Constants.COLUMN_NAME, "ColumnInfo").Where("ColumnInfo", "ColumnTypeId", 1).Where("ColumnInfo", Constants.COLUMN_INFO_TABLE_NAMES, tableName);
            return QueryDBGetString(query, databaseName)[0];
        }

        public Dictionary<string, List<Tuple<string, string>>> GetStructure(string databaseName)
        {
            throw new NotImplementedException();
        }

        public DatabaseConstants GetDatabaseConstants()
        {
            return Constants;
        }

    }
}
