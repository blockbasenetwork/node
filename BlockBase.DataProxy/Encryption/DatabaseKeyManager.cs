using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Utils.Crypto;
using System.Collections.Generic;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    public class DatabaseKeyManager
    {
        private readonly ISecretStore _keyStore;
        private readonly InfoRecordManager _infoRecordManager;
        public string LiteralDatabaseName { get; private set; }
        private readonly KeyAndIVGenerator_v2 _keyAndIVGenerator;

        public DatabaseKeyManager(string literalDatabaseName, ISecretStore keyStore)
        {
            LiteralDatabaseName = literalDatabaseName;
            _keyStore = keyStore;
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

        public List<InfoRecord> FindChildren(string iv, bool deepFind = false)
        {
            return _infoRecordManager.FindChildren(iv, deepFind);
        }

        public InfoRecord AddInfoRecord(estring name, bool isDatabaseRecord, byte[] parentManageKey, byte[] parentIV, string data = null)
        {
            var keyGenerator = new KeyAndIVGenerator_v2();

            var ivBytes = keyGenerator.CreateRandomIV();
            var keyManageBytes = keyGenerator.CreateDerivateKey(parentManageKey, parentIV);
            var keyNameBytes = keyGenerator.CreateDerivateKey(keyManageBytes, ivBytes);

            string recordName = !name.ToEncrypt ? name.Value : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(name.Value), keyNameBytes, ivBytes));
            var iv = Base32Encoding.ZBase32.GetString(ivBytes);
            var keyManage = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyManageBytes, keyManageBytes, ivBytes));
            var keyName = !name.ToEncrypt ? null : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyNameBytes, keyNameBytes, ivBytes));
            string pIV = isDatabaseRecord ? null : Base32Encoding.ZBase32.GetString(parentIV);
            string encryptedData = data == null ? null : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(data), keyManageBytes, ivBytes));

            var infoRecord = InfoRecordManager.CreateInfoRecord(name.Value, keyManage, keyName, iv, pIV);
            _infoRecordManager.AddInfoRecord(infoRecord);
            return infoRecord;
        }

        public byte[] GetKeyManageFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _keyStore.GetSecret(infoRecord.LocalNameHash);
            if (key == null) return null;

            var decryptedManageKeyData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyManage), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));

            if (!Utils.Crypto.Utils.AreByteArraysEqual(key, decryptedManageKeyData)) return null;

            return decryptedManageKeyData;
        }

        public byte[] GetKeyNameFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _keyStore.GetSecret(infoRecord.LocalNameHash);
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
    }
}