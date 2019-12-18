using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Utils.Crypto;
using Newtonsoft.Json;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    public class DatabaseKeyManager
    {
        private readonly ISecretStore _secretStore;
        private readonly InfoRecordManager _infoRecordManager;
        public string LiteralDatabaseName { get; private set; }
        private readonly KeyAndIVGenerator_v2 _keyAndIVGenerator;

        public DatabaseKeyManager(string literalDatabaseName, ISecretStore secretStore)
        {
            LiteralDatabaseName = literalDatabaseName;
            _secretStore = secretStore;
            _infoRecordManager = new InfoRecordManager();
            _keyAndIVGenerator = new KeyAndIVGenerator_v2();
        }

        public void SyncData()
        {
            var infoRecords = FetchInfoRecords();
            LoadInfoRecordsToRecordManager(infoRecords);
        }

        public InfoRecord FindInfoRecord(estring name, string parentIV)
        {
            return _infoRecordManager.FindInfoRecord(name, parentIV);
        }

        public InfoRecord FindInfoRecord(string recordIV)
        {
            return _infoRecordManager.FindInfoRecord(recordIV);
        }

        public List<InfoRecord> FindChildren(string iv, bool deepFind = false)
        {
            return _infoRecordManager.FindChildren(iv, deepFind);
        }

        public InfoRecord AddInfoRecord(estring name, InfoRecordTypeEnum recordTypeEnum, byte[] parentManageKey, byte[] parentIV, string data = null)
        {
            var keyGenerator = new KeyAndIVGenerator_v2();

            var ivBytes = keyGenerator.CreateRandomIV();
            var keyManageBytes = keyGenerator.CreateDerivateKey(parentManageKey, parentIV);
            var keyNameBytes = keyGenerator.CreateDerivateKey(keyManageBytes, ivBytes);

            string recordName = !name.ToEncrypt ? name.Value : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(name.Value), keyNameBytes, ivBytes));
            var iv = Base32Encoding.ZBase32.GetString(ivBytes);
            var keyManage = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyManageBytes, keyManageBytes, ivBytes));
            var keyName = !name.ToEncrypt ? null : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyNameBytes, keyNameBytes, ivBytes));
            string pIV = recordTypeEnum == InfoRecordTypeEnum.DatabaseRecord ? null : Base32Encoding.ZBase32.GetString(parentIV);
            string encryptedData = data == null ? null : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(data), keyManageBytes, ivBytes));

            InfoRecord.LocalData localData = null;
            if (recordTypeEnum == InfoRecordTypeEnum.ColumnRecord)
            {
                localData = new InfoRecord.LocalData();
                DataType dataType = JsonConvert.DeserializeObject<DataType>(data);
                string template = "_{0}{1}";
                if (dataType.BucketInfo.EqualityBucketSize.HasValue)
                {
                    localData.EncryptedEqualityColumnName = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(string.Format(template, "e", recordName)), keyManageBytes, ivBytes));
                    localData.EncryptedIVColumnName = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(string.Format(template, "i", recordName)), keyManageBytes, ivBytes));
                }
                if (dataType.BucketInfo.RangeBucketSize.HasValue)
                {
                    localData.EncryptedRangeColumnName = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(string.Format(template, "r", recordName)), keyManageBytes, ivBytes));
                }
            }

            var infoRecord = InfoRecordManager.CreateInfoRecord(name.Value, keyManage, keyName, iv, pIV, localData, encryptedData);
            _infoRecordManager.AddInfoRecord(infoRecord);
            return infoRecord;
        }

        public byte[] GetKeyManageFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _secretStore.GetSecret(infoRecord.LocalNameHash);
            if (key == null) return null;

            var decryptedManageKeyData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyManage), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));

            if (!Utils.Crypto.Utils.AreByteArraysEqual(key, decryptedManageKeyData)) return null;

            return decryptedManageKeyData;
        }

        public byte[] GetKeyNameFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _secretStore.GetSecret(infoRecord.LocalNameHash);
            if (key == null) return null;

            var decryptedKeyNameData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyName), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));

            if (!Utils.Crypto.Utils.AreByteArraysEqual(key, decryptedKeyNameData)) return null;

            return decryptedKeyNameData;
        }

        private void LoadInfoRecordsToRecordManager(List<InfoRecord> infoRecords)
        {
            foreach (var infoRecord in infoRecords)
            {
                TryAddLocalHashToInfoRecord(infoRecord);
                _infoRecordManager.AddInfoRecord(infoRecord);
            }
        }

        private void TryAddLocalHashToInfoRecord(InfoRecord infoRecord)
        {
            if (infoRecord.KeyName == null)
            {
                //the recordName isn't encrypted
                return;
            }

            //check if its keymanage

            var decryptedManageKeyData = GetKeyManageFromInfoRecord(infoRecord);
            var iv = Base32Encoding.ZBase32.ToBytes(infoRecord.IV);

            if (decryptedManageKeyData != null)
            {
                //user has the key manage
                //derive key name from key manage

                var keyName = _keyAndIVGenerator.CreateDerivateKey(decryptedManageKeyData, iv);
                var decryptedRecordNameInBytes = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.Name), keyName, iv);
                var localNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(decryptedRecordNameInBytes));

                infoRecord.LocalNameHash = localNameHash;
                return;
            }

            var decryptedKeyNameData = GetKeyNameFromInfoRecord(infoRecord);

            if (decryptedKeyNameData != null)
            {
                //user has the key read
                var decryptedRecordNameInBytes = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.Name), decryptedKeyNameData, iv);
                var localNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(decryptedRecordNameInBytes));

                infoRecord.LocalNameHash = localNameHash;

                return;
            }

            //user has no key
        }

        private List<InfoRecord> FetchInfoRecords()
        {
            //queries the database server to retrieve the infotable
            return new List<InfoRecord>();
        }

        //TODO: check if this is ok
        public DataType GetColumnDataType(InfoRecord columnInfoRecord)
        {
            if (columnInfoRecord.Data == null) throw new FormatException("Column Data is empty!");

            var keyManageBytes = GetKeyManageFromInfoRecord(columnInfoRecord);
            var ivBytes = Base32Encoding.ZBase32.ToBytes(columnInfoRecord.IV);

            var decryptedData = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(columnInfoRecord.Data), keyManageBytes, ivBytes));

            return JsonConvert.DeserializeObject<DataType>(decryptedData);
        }
        public InfoRecord ChangeInfoRecordName(InfoRecord infoRecord, estring newName)
        {
            var keyNameBytes = GetKeyNameFromInfoRecord(infoRecord);
            var ivBytes = Base32Encoding.ZBase32.ToBytes(infoRecord.IV);
            string newRecordName = !newName.ToEncrypt ? newName.Value : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(newName.Value), keyNameBytes, ivBytes));
            infoRecord.Name = newRecordName;
            return infoRecord;
        }

        public void RemoveInfoRecord(InfoRecord infoRecord)
        {
            _infoRecordManager.RemoveInfoRecord(infoRecord);
        }

        public string CreateEqualityBktValue(string value, string columnIV)
        {
            var columnInfoRecord = FindInfoRecord(columnIV);
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);
            var dataType = GetColumnDataType(columnInfoRecord);

            var valueBytes = Encoding.ASCII.GetBytes(value);
            var bucket = new BigInteger(Utils.Crypto.Utils.SHA256(AES256.EncryptWithCBC(valueBytes, columnManageKey, Base32Encoding.ZBase32.ToBytes(columnInfoRecord.IV)))) % dataType.BucketInfo.EqualityBucketSize.Value;

            return Base32Encoding.ZBase32.GetString(bucket.ToByteArray());
        }
        public string CreateRangeBktValue(double value, string columnIV)
        {
            var columnInfoRecord = FindInfoRecord(columnIV);
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);
            var dataType = GetColumnDataType(columnInfoRecord);

            var upperBound = CalculateUpperBound(dataType.BucketInfo.RangeBucketSize.Value,
                                                 dataType.BucketInfo.BucketMinRange.Value,
                                                 dataType.BucketInfo.BucketMaxRange.Value,
                                                 value);

            var upperBoundBytes = BitConverter.GetBytes(upperBound);
            var bucket = Utils.Crypto.Utils.SHA256(AES256.EncryptWithCBC(upperBoundBytes, columnManageKey, Base32Encoding.ZBase32.ToBytes(columnInfoRecord.IV)));
            return Base32Encoding.ZBase32.GetString(bucket);
        }
        private int CalculateUpperBound(int N, int min, int max, double value)
        {
            if (value < min || value > max) throw new ArgumentOutOfRangeException("The value you inserted is out of bounds.");

            for (int i = min + N - 1; i <= max; i += N)
            {
                if (value <= i)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException("The value you inserted is out of bounds.");
        }

        public string EncryptNormalValue(string valueToInsert, string columnIV, out string generatedIV)
        {
            var columnInfoRecord = FindInfoRecord(columnIV);
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);

            var randomIV = _keyAndIVGenerator.CreateRandomIV();
            generatedIV = Base32Encoding.ZBase32.GetString(randomIV);

            var valueInBytes = Encoding.ASCII.GetBytes(valueToInsert);
            return Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(valueInBytes, columnManageKey, randomIV));
        }
        public string EncryptUniqueValue(string valueToInsert, string columnIV)
        {
            var columnInfoRecord = FindInfoRecord(columnIV);
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);

            var valueInBytes = Encoding.ASCII.GetBytes(valueToInsert);
            return Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(valueInBytes, columnManageKey, Base32Encoding.ZBase32.ToBytes(columnInfoRecord.IV)));
        }

        public enum InfoRecordTypeEnum
        {
            DatabaseRecord,
            TableRecord,
            ColumnRecord,
            Unknown
        }

        
    }
}