using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Wiry.Base32;

namespace BlockBase.DataProxy
{
    public class MiddleMan : IEncryptor
    {
        public DatabaseKeyManager DatabaseKeyManager;

        public MiddleMan(DatabaseKeyManager databaseKeyManager)
        {
            DatabaseKeyManager = databaseKeyManager;
        }

        public InfoRecord CreateInfoRecord(estring name, string parentIV)
        {
            if (parentIV != null)
            {
                var parentInfoRecord = DatabaseKeyManager.FindInfoRecord(parentIV);
                var parentManageKey = DatabaseKeyManager.GetKeyManageFromInfoRecord(parentInfoRecord);
                return DatabaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.TableRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV));
            }
            return DatabaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.DatabaseRecord, DatabaseKeyManager.SecretStore.GetSecret(EncryptionConstants.MASTER_KEY), DatabaseKeyManager.SecretStore.GetSecret(EncryptionConstants.MASTER_IV));
        }
        public InfoRecord CreateColumnInfoRecord(estring name, string parentIV, DataType data)
        {
            var parentInfoRecord = DatabaseKeyManager.FindInfoRecord(parentIV);
            var parentManageKey = DatabaseKeyManager.GetKeyManageFromInfoRecord(parentInfoRecord);
            return DatabaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.ColumnRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV), JsonConvert.SerializeObject(data));
        }

        public InfoRecord FindInfoRecord(estring name, string parentIV)
        {
            return DatabaseKeyManager.FindInfoRecord(name, parentIV);
        }
        public List<InfoRecord> FindChildren(string iv, bool deepFind = false)
        {
            return DatabaseKeyManager.FindChildren(iv, deepFind);
        }
        public DataType GetColumnDataType(InfoRecord columnInfoRecord)
        {
            return DatabaseKeyManager.GetColumnDataType(columnInfoRecord);
        }

        public InfoRecord ChangeInfoRecordName(InfoRecord infoRecord, estring newName)
        {
            return DatabaseKeyManager.ChangeInfoRecordName(infoRecord, newName);
        }
        public void RemoveInfoRecord(InfoRecord infoRecord)
        {
            DatabaseKeyManager.RemoveInfoRecord(infoRecord);
        }

        public string CreateEqualityBktValue(string valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType)
        {
            return DatabaseKeyManager.CreateEqualityBktValue(valueToInsert, columnInfoRecord, columnDataType);
        }
        public string CreateRangeBktValue(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType)
        {
            return DatabaseKeyManager.CreateRangeBktValue(valueToInsert, columnInfoRecord, columnDataType);
        }
        public IList<string> GetRangeBktValues(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType, bool superior)
        {
             return DatabaseKeyManager.GetRangeBktValues(valueToInsert, columnInfoRecord, columnDataType, superior);
        }
        public string GetEqualRangeBktValue(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType)
        {
             return DatabaseKeyManager.GetEqualRangeBktValue(valueToInsert, columnInfoRecord, columnDataType);
        }

        public string EncryptNormalValue(string valueToInsert, InfoRecord columnInfoRecord, out string generatedIV)
        {
            return DatabaseKeyManager.EncryptNormalValue(valueToInsert, columnInfoRecord, out generatedIV);
        }
        public string EncryptUniqueValue(string valueToInsert, InfoRecord columnInfoRecord)
        {
            return DatabaseKeyManager.EncryptUniqueValue(valueToInsert, columnInfoRecord);
        }

        public string DecryptNormalValue(string encryptedValue, InfoRecord columnInfoRecord, string generatedIV)
        {
            return DatabaseKeyManager.DecryptValue(encryptedValue, columnInfoRecord, generatedIV);
        }
        public string DecryptUniqueValue(string encryptedValue, InfoRecord columnInfoRecord)
        {
            return DatabaseKeyManager.DecryptValue(encryptedValue, columnInfoRecord);
        }

        public string DecryptName(InfoRecord infoRecord)
        {
            return DatabaseKeyManager.DecryptName(infoRecord);
        }


    }
}