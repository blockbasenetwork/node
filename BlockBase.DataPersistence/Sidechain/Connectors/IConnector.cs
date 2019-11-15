using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;
using BlockBase.Domain.Database.Constants;
using BlockBase.Domain.Database.Operations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain.Connectors
{
    public interface IConnector
    {
        Task Execute(Dictionary<string, LinkedList<ISqlOperation>> commandsToExecute);
        Task CreateMetaInfo(string databaseName);
        Task<bool> CheckDatabase(string databaseName);
        Task CreateDatabase(string databaseName);
        bool CheckIfMetaInfoTableExist(string databaseName);
        
        //TODO: PROXY
        DatabaseConstants GetDatabaseConstants();
        string GetTableNameWithPrefix(string encryptedName);
        string GetColumnNameWithPrefix(string columnName);
        string GetBucketColumnNameWithPrefix(string columnName);
        string GetIVColumnNameWithPrefix(string columnName);
        List<string> QueryDBGetString(QueryBuilder query, string databaseName);
        IList<Tuple<string, byte[], byte[]>> GetIdentifiersWithBucket(string primaryKeyColumnName, string tableName, string valueColumn, string columnName, byte[] bucket, string databaseName);
        int QueryDBGetValues(string query, List<IResult> values, string databaseName);
        Tuple<ColumnType, string> GetColumnTypeAndId(string encryptedValueColumn, string encryptedTableName, string databaseName);
        Tuple<byte[], byte[]> GetBucket(string guidString, ColumnType type, string databaseName);
        List<IResult> GetAllBucketSize(string encryptedTableName, string databaseName);
        Tuple<byte[], byte[], bool> GetBucketMaxRange(string guidString, string databaseName);
        string GetPrimaryKey(string tableName, string databaseName);
        Dictionary<string, List<Tuple<string, string>>> GetStructure(string databaseName);
    }
}
