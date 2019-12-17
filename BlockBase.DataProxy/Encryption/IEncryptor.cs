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
        InfoRecord CreateRangeBktColumnName(string columnName, int? size, int? min, int? max);

        void RemoveInfoRecord(string iv);

        Tuple<string, string> ChangeInfoRecord(estring oldName, estring newName, string parentIV);

        InfoRecord FindInfoRecord(estring name, string parentIV);

        List<InfoRecord> FindChildren(string parentIV, bool deepFind = false);


        estring GetIVColumnName(string columnName);

        Dictionary<string, string> GetColumnDatatypes(string tableName, string databaseName);

        string CreateRangeBktValue(string rangeColumnName, string valueToInsert, string columnName);
        string CreateEqualityBktValue(string rangeColumnName, string valueToInsert, string columnName);

        string EncryptNormalValue(string valueToInsert, string columnName, out string generatedIV);
        string EncryptUniqueValue(string valueToInsert, string columnName);
    }
}
