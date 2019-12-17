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

        InfoRecord FindInfoRecord(estring name, string parentIV);
        List<InfoRecord> FindChildren(string parentIV, bool deepFind = false);

        void RemoveInfoRecord(string iv);

        InfoRecord CreateColumnInfoRecord(estring name, string parentIV, DataType data);

        Dictionary<string, string> GetColumnDatatypes(string tableName, string databaseName);


        Tuple<string, string> ChangeInfoRecord(estring oldName, estring newName, string parentIV);

        estring GetIVColumnName(string columnName);

        

        string CreateRangeBktValue(string rangeColumnName, string valueToInsert, string columnName);
        string CreateEqualityBktValue(string rangeColumnName, string valueToInsert, string columnName);

        string EncryptNormalValue(string valueToInsert, string columnName, out string generatedIV);
        string EncryptUniqueValue(string valueToInsert, string columnName);
    }
}
