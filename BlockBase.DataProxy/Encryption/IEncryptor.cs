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

        void RemoveInfoRecord(InfoRecord infoRecord);

        InfoRecord CreateColumnInfoRecord(estring name, string parentIV, DataType data);
        string DecryptName(InfoRecord infoRecord);

        DataType GetColumnDataType(InfoRecord columnInfoRecord);

        InfoRecord ChangeInfoRecordName(InfoRecord infoRecord, estring newName);


        string CreateRangeBktValue(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDatatype);
        IList<string> GetRangeBktValues(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDatatype, bool superior);
        string CreateEqualityBktValue(string valueToInsert, InfoRecord columnInfoRecord, DataType columnDatatype);

        string EncryptNormalValue(string valueToInsert, InfoRecord columnInfoRecord, out string generatedIV);
        string EncryptUniqueValue(string valueToInsert, InfoRecord columnInfoRecord);

        string DecryptNormalValue(string encryptedValue, InfoRecord columnInfoRecord, string generatedIV);
        string DecryptUniqueValue(string encryptedValue, InfoRecord columnInfoRecord);
    }
}
