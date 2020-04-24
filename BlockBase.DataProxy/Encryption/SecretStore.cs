using System;
using System.Collections.Generic;
using Wiry.Base32;
using Microsoft.Extensions.Logging;
using System.Linq;
using BlockBase.Utils;
using System.Text;
using System.Security.Cryptography;
using BlockBase.Utils.Crypto;

namespace BlockBase.DataProxy.Encryption
{
    //TODO - this key store is a local in memory implementation of key storage that should be substituted by another implementation in the future
    public class SecretStore : ISecretStore
    {
        private Dictionary<string, byte[]> _secretStoreDict = new Dictionary<string, byte[]>();
        private static readonly string keysFileName = "keys.txt";
        private ILogger _logger;
        private byte[] _key;
        private byte[] _iv;

        public SecretStore(ILogger logger, string filePassword)
        {
            _logger = logger;
            GetHashKeys(filePassword);
            LoadSecrets();
        }

        public void SetSecret(string secretId, byte[] key)
        {
            if (_secretStoreDict.ContainsKey(secretId) && _secretStoreDict[secretId].SequenceEqual(key)) return;

            if (_secretStoreDict.ContainsKey(secretId)) throw new Exception("There's already a key to that IV.");

            _secretStoreDict.Add(secretId, key);
            var iv = secretId != EncryptionConstants.MASTER_KEY && secretId != EncryptionConstants.MASTER_IV ? Base32Encoding.ZBase32.ToBytes(secretId) : _iv;
            FileWriterReader.Write(keysFileName, secretId + ":" + Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(key, _key, iv)), System.IO.FileMode.Append);
        }

        public void LoadSecrets()
        {
            var fileLines = FileWriterReader.Read(keysFileName);
            foreach (var line in fileLines)
            {
                var idKey = line.Split(":");
                var iv = idKey[0] != EncryptionConstants.MASTER_KEY && idKey[0] != EncryptionConstants.MASTER_IV ? Base32Encoding.ZBase32.ToBytes(idKey[0]) : _iv;
                var encryptedData = Base32Encoding.ZBase32.ToBytes(idKey[1]);
                var decryptedKey = AES256.DecryptWithCBC(encryptedData, _key, iv);

                _secretStoreDict.Add(idKey[0], decryptedKey);
            }
        }

        public byte[] GetSecret(string secretId)
        {
            if (_secretStoreDict.ContainsKey(secretId)) return _secretStoreDict[secretId];
            return null;
        }

        private void GetHashKeys(string password)
        {
            byte[][] result = new byte[2][];
            Encoding enc = Encoding.UTF8;

            SHA256 sha2 = new SHA256CryptoServiceProvider();

            byte[] rawPass = enc.GetBytes(password);

            _key = sha2.ComputeHash(rawPass);
            byte[] hashIV = sha2.ComputeHash(rawPass);
 
            Array.Resize( ref hashIV, 16 );

            _iv = hashIV;
        }
    }
}