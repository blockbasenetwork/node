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
        private DatabaseKeyManager _databaseKeyManager;

        public MiddleMan(DatabaseKeyManager databaseKeyManager)
        {
            _databaseKeyManager = databaseKeyManager;
        }

        public InfoRecord CreateInfoRecord(estring name, string parentIV)
        {
            if (parentIV != null)
            {
                var parentInfoRecord = _databaseKeyManager.FindInfoRecord(parentIV);
                var parentManageKey = _databaseKeyManager.GetKeyManageFromInfoRecord(parentInfoRecord);
                return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.TableRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV));
            }
            return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.DatabaseRecord, _databaseKeyManager.SecretStore.GetSecret(EncryptionConstants.MASTER_KEY), _databaseKeyManager.SecretStore.GetSecret(EncryptionConstants.MASTER_IV));
        }
        public InfoRecord CreateColumnInfoRecord(estring name, string parentIV, DataType data)
        {
            var parentInfoRecord = _databaseKeyManager.FindInfoRecord(parentIV);
            var parentManageKey = _databaseKeyManager.GetKeyManageFromInfoRecord(parentInfoRecord);
            return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.ColumnRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV), JsonConvert.SerializeObject(data));
        }

        public InfoRecord FindInfoRecord(estring name, string parentIV)
        {
            return _databaseKeyManager.FindInfoRecord(name, parentIV);
        }
        public List<InfoRecord> FindChildren(string iv, bool deepFind = false)
        {
            return _databaseKeyManager.FindChildren(iv, deepFind);
        }
        public DataType GetColumnDataType(InfoRecord columnInfoRecord)
        {
            return _databaseKeyManager.GetColumnDataType(columnInfoRecord);
        }

        public InfoRecord ChangeInfoRecordName(InfoRecord infoRecord, estring newName)
        {
            return _databaseKeyManager.ChangeInfoRecordName(infoRecord, newName);
        }
        public void RemoveInfoRecord(InfoRecord infoRecord)
        {
            _databaseKeyManager.RemoveInfoRecord(infoRecord);
        }

        public string CreateEqualityBktValue(string valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType)
        {
            return _databaseKeyManager.CreateEqualityBktValue(valueToInsert, columnInfoRecord, columnDataType);
        }
        public string CreateRangeBktValue(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType)
        {
            return _databaseKeyManager.CreateRangeBktValue(valueToInsert, columnInfoRecord, columnDataType);
        }
        public IList<string> GetRangeBktValues(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType, bool superior)
        {
             return _databaseKeyManager.GetRangeBktValues(valueToInsert, columnInfoRecord, columnDataType, superior);
        }
        public string GetEqualRangeBktValue(double valueToInsert, InfoRecord columnInfoRecord, DataType columnDataType)
        {
             return _databaseKeyManager.GetEqualRangeBktValue(valueToInsert, columnInfoRecord, columnDataType);
        }

        public string EncryptNormalValue(string valueToInsert, InfoRecord columnInfoRecord, out string generatedIV)
        {
            return _databaseKeyManager.EncryptNormalValue(valueToInsert, columnInfoRecord, out generatedIV);
        }
        public string EncryptUniqueValue(string valueToInsert, InfoRecord columnInfoRecord)
        {
            return _databaseKeyManager.EncryptUniqueValue(valueToInsert, columnInfoRecord);
        }

        public string DecryptNormalValue(string encryptedValue, InfoRecord columnInfoRecord, string generatedIV)
        {
            return _databaseKeyManager.DecryptValue(encryptedValue, columnInfoRecord, generatedIV);
        }
        public string DecryptUniqueValue(string encryptedValue, InfoRecord columnInfoRecord)
        {
            return _databaseKeyManager.DecryptValue(encryptedValue, columnInfoRecord);
        }

        public string DecryptName(InfoRecord infoRecord)
        {
            return _databaseKeyManager.DecryptName(infoRecord);
        }


    }
}