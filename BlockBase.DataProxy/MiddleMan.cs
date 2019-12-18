using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Wiry.Base32;

namespace BlockBase.DataProxy
{
    internal class MiddleMan : IEncryptor
    {
        private DatabaseKeyManager _databaseKeyManager;
        private SecretStore _keyStore;

        public MiddleMan(DatabaseKeyManager databaseKeyManager, SecretStore keyStore)
        {
            _databaseKeyManager = databaseKeyManager;
            _keyStore = keyStore;
        }

        public Tuple<string, string> ChangeInfoRecord(estring oldName, estring newName, string parentIV)
        {
            throw new NotImplementedException();
        }

        public InfoRecord CreateColumnInfoRecord(estring name, string parentIV, DataType data)
        {
            var parentManageKey = _keyStore.GetSecret(parentIV);
            return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.ColumnRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV), JsonConvert.SerializeObject(data));
        }

        public string CreateEqualityBktValue(string valueToInsert, string columnIV)
        {
            throw new NotImplementedException();
        }

        public InfoRecord CreateInfoRecord(estring name, string parentIV)
        {
            if (_databaseKeyManager.FindInfoRecord(name, parentIV) == null)
            {
                if (parentIV != null)
                {
                    var parentManageKey = _keyStore.GetSecret(parentIV);
                    return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.TableRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV));
                }
                return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.DatabaseRecord, _keyStore.GetSecret("master_key"), _keyStore.GetSecret("master_iv"));
            }
            return null;
        }

        public string CreateRangeBktValue(string valueToInsert, string columnIV)
        {
            throw new NotImplementedException();
        }

        public string EncryptNormalValue(string valueToInsert, string columnIV, out string generatedIV)
        {
            throw new NotImplementedException();
        }

        public string EncryptUniqueValue(string valueToInsert, string columnIV)
        {
            throw new NotImplementedException();
        }

        public List<InfoRecord> FindChildren(string parentIV, bool deepFind = false)
        {
            throw new NotImplementedException();
        }

        public InfoRecord FindInfoRecord(estring name, string parentIV)
        {
            throw new NotImplementedException();
        }

        public DataType GetColumnDatatype(string columnIV)
        {
            throw new NotImplementedException();
        }

        public void RemoveInfoRecord(string iv)
        {
            throw new NotImplementedException();
        }
    }
}