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

        InfoRecord RemoveInfoRecord(estring databaseName);
        InfoRecord RemoveInfoRecord(estring tableName, string databaseName);
        InfoRecord RemoveInfoRecord(estring columnName, string tableName, string databaseName);

        Tuple<string, string> ChangeInfoRecord(estring oldTableName, estring newTableName, string databaseName);

        string GetEncryptedName(estring databaseName);
        string GetEncryptedName(estring tableName, string databaseName);
        string GetEncryptedName(estring columnName, string tableName, string databaseName);

        string GetEncryptedBucketColumn(string bktValues);
    }
}
