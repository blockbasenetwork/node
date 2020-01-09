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

        public DatabaseKeyManager(ISecretStore secretStore)
        {
            _secretStore = secretStore;
            _infoRecordManager = new InfoRecordManager();
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
            var ivBytes = KeyAndIVGenerator_v2.CreateRandomIV();
            var keyManageBytes = KeyAndIVGenerator_v2.CreateDerivateKey(parentManageKey, parentIV);
            var keyNameBytes = KeyAndIVGenerator_v2.CreateDerivateKey(keyManageBytes, ivBytes);

            var localNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(Encoding.Unicode.GetBytes(name.Value)));
            string recordName = !name.ToEncrypt ? name.Value : "_" + Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(name.Value), keyNameBytes, ivBytes));
            var iv = Base32Encoding.ZBase32.GetString(ivBytes);
            var keyManage = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyManageBytes, keyManageBytes, ivBytes));
            var keyName = !name.ToEncrypt ? null : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyNameBytes, keyNameBytes, ivBytes));
            string pIV = recordTypeEnum == InfoRecordTypeEnum.DatabaseRecord ? null : Base32Encoding.ZBase32.GetString(parentIV);
            string encryptedData = data == null ? null : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(data), keyManageBytes, ivBytes));

            _secretStore.SetSecret(iv, keyManageBytes);

            InfoRecord.LocalData localData = null;
            if (recordTypeEnum == InfoRecordTypeEnum.ColumnRecord)
            {
                localData = new InfoRecord.LocalData();
                DataType dataType = JsonConvert.DeserializeObject<DataType>(data);
                string template = "{0}{1}";
                if (dataType.BucketInfo.EqualityBucketSize.HasValue)
                {
                    localData.EncryptedEqualityColumnName = "_" + Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(string.Format(template, "e", recordName)), keyManageBytes, ivBytes));
                    localData.EncryptedIVColumnName = "_" + Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(string.Format(template, "i", recordName)), keyManageBytes, ivBytes));
                }
                if (dataType.BucketInfo.RangeBucketSize.HasValue)
                {
                    localData.EncryptedRangeColumnName = "_" + Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(string.Format(template, "r", recordName)), keyManageBytes, ivBytes));
                }
            }

            var infoRecord = InfoRecordManager.CreateInfoRecord(recordName, keyManage, keyName, iv, pIV, localNameHash, localData, encryptedData);
            _infoRecordManager.AddInfoRecord(infoRecord);
            return infoRecord;
        }

        public byte[] GetKeyManageFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _secretStore.GetSecret(infoRecord.IV);
            if (key == null) return null;

            var decryptedManageKeyData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyManage), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));

            if (!Utils.Crypto.Utils.AreByteArraysEqual(key, decryptedManageKeyData)) return null;

            return decryptedManageKeyData;
        }

        public byte[] GetKeyNameFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _secretStore.GetSecret(infoRecord.IV);
            if (key == null) return null;
            byte[] decryptedKeyNameData;
            try
            {
                decryptedKeyNameData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyName), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));
                return decryptedKeyNameData;
            }


            catch (System.Security.Cryptography.CryptographicException)
            {
                var derivatedKeyRead = KeyAndIVGenerator_v2.CreateDerivateKey(key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));
                decryptedKeyNameData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyName), derivatedKeyRead, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));
                return decryptedKeyNameData;
            }
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

                var keyName = KeyAndIVGenerator_v2.CreateDerivateKey(decryptedManageKeyData, iv);
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
            var decryptedData = Encoding.Unicode.GetString(AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(columnInfoRecord.Data), keyManageBytes, ivBytes));

            return JsonConvert.DeserializeObject<DataType>(decryptedData);
        }
        public InfoRecord ChangeInfoRecordName(InfoRecord infoRecord, estring newName)
        {
            if (newName.ToEncrypt)
            {
                byte[] keyNameBytes;
                var ivBytes = Base32Encoding.ZBase32.ToBytes(infoRecord.IV);
                if (infoRecord.KeyName != null) keyNameBytes = GetKeyNameFromInfoRecord(infoRecord);
                else
                {
                    keyNameBytes = KeyAndIVGenerator_v2.CreateDerivateKey(GetKeyManageFromInfoRecord(infoRecord), ivBytes);
                    infoRecord.KeyName = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyNameBytes, keyNameBytes, ivBytes));
                }
                infoRecord.Name = "_" + Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(newName.Value), keyNameBytes, ivBytes));
            }
            else
            {
                infoRecord.Name = newName.Value;
                infoRecord.KeyName = null;
            }           
            infoRecord.LocalNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(Encoding.Unicode.GetBytes(newName.Value)));
            return infoRecord;
        }

        public void RemoveInfoRecord(InfoRecord infoRecord)
        {
            _infoRecordManager.RemoveInfoRecord(infoRecord);
        }

        public string CreateEqualityBktValue(string value, InfoRecord columnInfoRecord, DataType columnDataType)
        {
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);

            var valueBytes = Encoding.ASCII.GetBytes(value);
            var bucket = new BigInteger(Utils.Crypto.Utils.SHA256(AES256.EncryptWithCBC(valueBytes, columnManageKey, Base32Encoding.ZBase32.ToBytes(columnInfoRecord.IV)))) % columnDataType.BucketInfo.EqualityBucketSize.Value;

            return Base32Encoding.ZBase32.GetString(bucket.ToByteArray());
        }
        public string CreateRangeBktValue(double value, InfoRecord columnInfoRecord, DataType columnDataType)
        {
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);

            var upperBound = CalculateUpperBound(columnDataType.BucketInfo.RangeBucketSize.Value,
                                                 columnDataType.BucketInfo.BucketMinRange.Value,
                                                 columnDataType.BucketInfo.BucketMaxRange.Value,
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

        public string EncryptNormalValue(string valueToInsert, InfoRecord columnInfoRecord, out string generatedIV)
        {
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);

            var randomIV = KeyAndIVGenerator_v2.CreateRandomIV();
            generatedIV = Base32Encoding.ZBase32.GetString(randomIV);

            var valueInBytes = Encoding.ASCII.GetBytes(valueToInsert);
            return Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(valueInBytes, columnManageKey, randomIV));
        }
        public string EncryptUniqueValue(string valueToInsert, InfoRecord columnInfoRecord)
        {
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);

            var valueInBytes = Encoding.ASCII.GetBytes(valueToInsert);
            return Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(valueInBytes, columnManageKey, Base32Encoding.ZBase32.ToBytes(columnInfoRecord.IV)));
        }

        public string DecryptValue(string encryptedValue, InfoRecord columnInfoRecord, string generatedIV = null)
        {
            var columnManageKey = GetKeyManageFromInfoRecord(columnInfoRecord);
            var encryptedValueInBytes = Base32Encoding.ZBase32.ToBytes(encryptedValue);

            var iv = generatedIV != null ? generatedIV : columnInfoRecord.IV;

            return Encoding.ASCII.GetString(AES256.DecryptWithCBC(encryptedValueInBytes, columnManageKey, Base32Encoding.ZBase32.ToBytes(iv)));
        }
        public string DecryptName(InfoRecord infoRecord)
        {
            var keyName = GetKeyNameFromInfoRecord(infoRecord);
            string recordName = infoRecord.Name.Substring(1);
            return Encoding.Unicode.GetString(AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(recordName), keyName, Base32Encoding.ZBase32.ToBytes(infoRecord.IV)));
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