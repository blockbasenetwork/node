using BlockBase.Domain.Database.Info;
using BlockBase.Utils.Crypto;
using System.Collections.Generic;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    public class Encryptor_v2
    {
        private Dictionary<string, InfoRecord> _infoRecordsDict = new Dictionary<string, InfoRecord>();

        public InfoRecord CreateInfoRecord(string recordName, bool encryptName, byte[] parentKey, byte[] parentIV)
        {
            var keyGenerator = new KeyAndIVGenerator_v2();

            var iv = keyGenerator.CreateRandomIV();
            var keyManage = keyGenerator.CreateDerivateKey(parentKey, parentIV);
            var keyRead = keyGenerator.CreateDerivateKey(keyManage, iv);

            return new InfoRecord
            {
                Name = !encryptName ? recordName : Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(Encoding.Unicode.GetBytes(recordName), keyRead, iv)),
                IV = Base32Encoding.ZBase32.GetString(iv),
                KeyManage = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyManage, keyManage, iv)),
                KeyRead = Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(keyRead, keyManage, iv)),
            };
        }

        public InfoRecord FindInfoRecord(string iv)
        {
            if (_infoRecordsDict.ContainsKey(iv))
                return _infoRecordsDict[iv];
            return null;
        }

        public void SaveInfoRecord(InfoRecord infoRecord, InfoRecord parent)
        {
            parent?.Children.Add(infoRecord);
            infoRecord.Parent = parent?.IV;
            _infoRecordsDict.Add(infoRecord.IV, infoRecord);
        }

        public void ClearRecords()
        {
            _infoRecordsDict = new Dictionary<string, InfoRecord>();
        }
    }
}