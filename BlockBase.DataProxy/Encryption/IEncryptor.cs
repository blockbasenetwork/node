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

        DataType GetColumnDatatype(InfoRecord columnInfoRecord);

        Tuple<string, string> ChangeInfoRecord(estring oldName, estring newName, string parentIV);


        string CreateRangeBktValue(double valueToInsert, string columnIV);
        string CreateEqualityBktValue(string valueToInsert, string columnIV);

        string EncryptNormalValue(string valueToInsert, string columnIV, out string generatedIV);
        string EncryptUniqueValue(string valueToInsert, string columnIV);
    }
}
