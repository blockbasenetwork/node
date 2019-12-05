using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Utils.Crypto;
using System.Collections.Generic;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    public class InfoRecordManager
    {
        private List<InfoRecord> _infoRecords = new List<InfoRecord>();
        private Dictionary<string, InfoRecord> _infoRecordsLocalNameHashLookup = new Dictionary<string, InfoRecord>();
        private Dictionary<string, InfoRecord> _infoRecordsNameLookup = new Dictionary<string, InfoRecord>();

        public static InfoRecord CreateInfoRecord(estring recordName, byte[] parentManageKey, byte[] parentIV)
        {
            var keyGenerator = new KeyAndIVGenerator_v2();

            var iv = keyGenerator.CreateRandomIV();
            var keyManage = keyGenerator.CreateDerivateKey(parentManageKey, parentIV);
            var keyRead = keyGenerator.CreateDerivateKey(keyManage, iv);

            return new InfoRecord
            {
                Name = !recordName.ToEncrypt ? recordName.Value : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(recordName.Value), keyRead, iv)),
                IV = Base32Encoding.ZBase32.GetString(iv),
                KeyManage = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyManage, keyManage, iv)),
                KeyRead = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyRead, keyRead, iv)),
                NameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(Utils.Crypto.Utils.ConcatenateByteArray(Encoding.Unicode.GetBytes(recordName.Value), iv))),
                LocalNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(Encoding.Unicode.GetBytes(recordName.Value)))
            };
        }

        public InfoRecord FindInfoRecord(string recordName)
        {
            //if recordName is unencrypted but stored name is encrypted, try to retrieve the record through its hash
            var localNameHash = Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(Encoding.Unicode.GetBytes(recordName)));
            if (_infoRecordsLocalNameHashLookup.ContainsKey(localNameHash))
                return _infoRecordsLocalNameHashLookup[localNameHash];

            //if recordName is not found through its hash, try to find it directly
            if (_infoRecordsNameLookup.ContainsKey(recordName))
                return _infoRecordsNameLookup[recordName];

            return null;
        }

        public IEnumerable<InfoRecord> GetAllInfoRecords()
        {
            return _infoRecords;
        }

        public void AddInfoRecord(InfoRecord infoRecord, InfoRecord parent)
        {
            parent?.Children.Add(infoRecord);
            infoRecord.Parent = parent?.IV;
            if (!string.IsNullOrWhiteSpace(infoRecord.LocalNameHash))
                _infoRecordsLocalNameHashLookup.Add(infoRecord.LocalNameHash, infoRecord);
            _infoRecordsNameLookup.Add(infoRecord.Name, infoRecord);

            _infoRecords = new List<InfoRecord>();
        }

        public void ClearRecords()
        {
            _infoRecords = new List<InfoRecord>();
            _infoRecordsLocalNameHashLookup = new Dictionary<string, InfoRecord>();
            _infoRecordsNameLookup = new Dictionary<string, InfoRecord>();
        }
    }
}