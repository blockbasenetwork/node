using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Utils.Crypto;
using System.Collections.Generic;
using System.Linq;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    public class DatabaseKeyManager
    {
        private readonly IKeyStore _keyStore;
        private readonly InfoRecordManager _infoRecordManager;
        public string LiteralDatabaseName { get; private set; }
        private readonly KeyAndIVGenerator_v2 _keyAndIVGenerator;

        public DatabaseKeyManager(string literalDatabaseName, IKeyStore keyStore)
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

        public InfoRecord FindInfoRecord(string name)
        {
            return _infoRecordManager.FindInfoRecord(name);
        }

        public void AddInfoRecord(estring name, estring parentName, byte[] parentManageKey, byte[] parentIV)
        {
            var parentInfoRecord = FindInfoRecord(parentName.Value);
            _infoRecordManager.AddInfoRecord(InfoRecordManager.CreateInfoRecord(name, parentManageKey, parentIV), parentInfoRecord);
        }

        public byte[] GetKeyManageFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _keyStore.GetKey(infoRecord.LocalNameHash);
            if (key == null) return null;

            var decryptedManageKeyData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyManage), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));

            if (!Utils.Crypto.Utils.AreByteArraysEqual(key, decryptedManageKeyData)) return null;

            return decryptedManageKeyData;
        }

        public byte[] GetKeyReadFromInfoRecord(InfoRecord infoRecord)
        {
            var key = _keyStore.GetKey(infoRecord.LocalNameHash);
            if (key == null) return null;

            var decryptedReadKeyData = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.KeyRead), key, Base32Encoding.ZBase32.ToBytes(infoRecord.IV));

            if (!Utils.Crypto.Utils.AreByteArraysEqual(key, decryptedReadKeyData)) return null;

            return decryptedReadKeyData;
        }

        private void LoadInfoRecordsToRecordManager(List<InfoRecord> infoRecords)
        {
            foreach (var infoRecord in infoRecords)
            {
                TryAddLocalHashToInfoRecord(infoRecord);
                var parent = infoRecords.SingleOrDefault(r => r.IV == infoRecord.Parent);
                _infoRecordManager.AddInfoRecord(infoRecord, parent);
            }
        }

        private void TryAddLocalHashToInfoRecord(InfoRecord infoRecord)
        {
            if (infoRecord.KeyRead == null)
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
                //derive key read from key manage

                var keyRead = _keyAndIVGenerator.CreateDerivateKey(decryptedManageKeyData, iv);
                var decryptedRecordNameInBytes = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.Name), keyRead, iv);
                var localNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(decryptedRecordNameInBytes));

                infoRecord.LocalNameHash = localNameHash;
                return;
            }

            var decryptedReadKeyData = GetKeyManageFromInfoRecord(infoRecord);

            if (decryptedReadKeyData != null)
            {
                //user has the key read
                var decryptedRecordNameInBytes = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(infoRecord.Name), decryptedReadKeyData, iv);
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