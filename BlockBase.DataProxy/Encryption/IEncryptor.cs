using System;
using System.Collections.Generic;
using System.Text;

using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.DataProxy.Encryption
{
    public interface IEncryptor
    {
        InfoRecord CreateInfoRecord(estring name, string parentIV);
    
        InfoRecord CreateEqualityBktColumnName(string columnName, int? size);
        //{
        //    var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size;
        //    var encryptedSizeAndRange = _encryptor.GetEncryptedBucketColumn(bucketColumnNameString);
        //    return new estring(_equalityBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange, false);
        //}
        InfoRecord CreateRangeBktColumnName(string columnName, int? size, int? min, int? max);
        //{
        //    var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size + _separatingChar + min + _separatingChar + max;
        //    var encryptedSizeAndRange = _encryptor.GetEncryptedBucketColumn(bucketColumnNameString);

        //    return new estring(_rangeBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange, false);
        //}

        InfoRecord RemoveInfoRecord(estring name, string parentIV);

        Tuple<string, string> ChangeInfoRecord(estring oldName, estring newName, string parentIV);

        InfoRecord FindInfoRecord(estring name, string parentIV);

      
        Tuple<string, string> GetEncryptedBktColumnNames(string columnIV);
        estring GetIVColumnName(string columnName);
        //{
        //    return new estring(_ivPrefix + _separatingChar + columnName, false);
        //}

        Dictionary<string, string> GetColumnDatatypes(string tableName, string databaseName);

        
        

        string CreateRangeBktValue(string rangeColumnName, string valueToInsert, string columnName);
        string CreateEqualityBktValue(string rangeColumnName, string valueToInsert, string columnName);

        string EncryptNormalValue(string valueToInsert, string columnName, out string generatedIV);
        string EncryptUniqueValue(string valueToInsert, string columnName);
    }
}
