﻿using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy
{
    class MiddleMan : IEncryptor
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

        

        public string CreateEqualityBktValue(string rangeColumnName, string valueToInsert, string columnName)
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
                    return _databaseKeyManager.AddInfoRecord(name, false, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV));
                }
                return _databaseKeyManager.AddInfoRecord(name, true, _keyStore.GetSecret("master_key"), _keyStore.GetSecret("master_iv"));
            }
            return null;
        }

        public InfoRecord CreateEqualityBktInfoRecord(string columnIV, int size)
        {
            throw new NotImplementedException();
        }

        public InfoRecord CreateRangeBktInfoRecord(string columnIV, int size, int min, int max)
        {
            throw new NotImplementedException();
        }

        public string CreateRangeBktValue(string rangeColumnName, string valueToInsert, string columnName)
        {
            throw new NotImplementedException();
        }

        public string EncryptNormalValue(string valueToInsert, string columnName, out string generatedIV)
        {
            throw new NotImplementedException();
        }

        public string EncryptUniqueValue(string valueToInsert, string columnName)
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

        public Dictionary<string, string> GetColumnDatatypes(string tableName, string databaseName)
        {
            throw new NotImplementedException();
        }

        public estring GetIVColumnName(string columnName)
        {
            throw new NotImplementedException();
        }

        public void RemoveInfoRecord(string iv)
        {
            throw new NotImplementedException();
        }
    }
}
