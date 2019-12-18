﻿using BlockBase.DataProxy.Encryption;
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

        public InfoRecord CreateInfoRecord(estring name, string parentIV)
        {
            if (_databaseKeyManager.FindInfoRecord(name, parentIV) == null)
            {
                if (parentIV != null)
                {
                    var parentInfoRecord = _databaseKeyManager.FindInfoRecord(parentIV);
                    var parentManageKey = _databaseKeyManager.GetKeyManageFromInfoRecord(parentInfoRecord);
                    return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.TableRecord, parentManageKey, Base32Encoding.ZBase32.ToBytes(parentIV));
                }
                return _databaseKeyManager.AddInfoRecord(name, DatabaseKeyManager.InfoRecordTypeEnum.DatabaseRecord, _keyStore.GetSecret("master_key"), _keyStore.GetSecret("master_iv"));
            }
            return null;
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
        
        public string CreateEqualityBktValue(string valueToInsert, string columnIV)
        {
            return _databaseKeyManager.CreateEqualityBktValue(valueToInsert, columnIV);
        }
        public string CreateRangeBktValue(double valueToInsert, string columnIV)
        {
            return _databaseKeyManager.CreateRangeBktValue(valueToInsert, columnIV);
        }

        public string EncryptNormalValue(string valueToInsert, string columnIV, out string generatedIV)
        {
            return _databaseKeyManager.EncryptNormalValue(valueToInsert, columnIV, out generatedIV);
        }
        public string EncryptUniqueValue(string valueToInsert, string columnIV)
        {
            return _databaseKeyManager.EncryptUniqueValue(valueToInsert, columnIV);
        }
    }
}