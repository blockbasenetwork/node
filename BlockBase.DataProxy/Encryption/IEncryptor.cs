using System;
using System.Collections.Generic;
using System.Text;

using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.DataProxy.Encryption
{
    public interface IEncryptor
    {
        InfoRecord CreateInfoRecord(estring databaseName);
        InfoRecord CreateInfoRecord(estring tableName, string databaseName);
        InfoRecord CreateInfoRecord(estring columnName, string tableName, string databaseName, bool isDataEncrypted);

        string RemoveInfoRecord(estring databaseName);
        string RemoveInfoRecord(estring tableName, string databaseName);
        IList<string> RemoveInfoRecord(estring columnName, string tableName, string databaseName);

        Tuple<string, string> ChangeInfoRecord(estring oldTableName, estring newTableName, string databaseName);

        string GetEncryptedDatabaseName(estring databaseName);
        string GetEncryptedTableName(estring tableName, string databaseName);
        string GetEncryptedColumnName(estring columnName, string tableName, string databaseName);
        Tuple<string, string> GetEncryptedBktColumnNames(estring columnName, string tableName, string databaseName);

        string GetRangeBucket(string columnName, int upperBound);

        Dictionary<string, string> GetColumnDatatypes(string tableName, string databaseName);

        string GetEncryptedBucketColumn(string bktValues);
    }
}
