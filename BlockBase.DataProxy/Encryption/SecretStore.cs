using System;
using System.Collections.Generic;
using Wiry.Base32;
using Microsoft.Extensions.Logging;
using System.Linq;
using BlockBase.Utils;

namespace BlockBase.DataProxy.Encryption
{
    //TODO - this key store is a local in memory implementation of key storage that should be substituted by another implementation in the future
    public class SecretStore : ISecretStore
    {
        private Dictionary<string, byte[]> _secretStoreDict = new Dictionary<string, byte[]>();
        private static readonly string keysFileName = "keys.txt";
        private ILogger _logger;
        private string _filePassword;

        public SecretStore(ILogger logger, string filePassword)
        {
            _logger = logger;
            _filePassword = filePassword;
            LoadSecrets();
        }

        public void SetSecret(string secretId, byte[] key)
        {
            if (_secretStoreDict.ContainsKey(secretId) && _secretStoreDict[secretId].SequenceEqual(key)) return;

            if (_secretStoreDict.ContainsKey(secretId))
                throw new Exception("There's already a key to that IV.");

            _secretStoreDict.Add(secretId, key);
            FileWriterReader.Write(keysFileName, secretId + ":" + Base32Encoding.ZBase32.GetString(key), System.IO.FileMode.Append);
        }

        public void LoadSecrets()
        {
            var fileLines = FileWriterReader.Read(keysFileName);
            foreach (var line in fileLines)
            {
                var idKey = line.Split(":");
                _secretStoreDict.Add(idKey[0], Base32Encoding.ZBase32.ToBytes(idKey[1]));
            }
        }

        public byte[] GetSecret(string secretId)
        {
            if (_secretStoreDict.ContainsKey(secretId)) return _secretStoreDict[secretId];
            return null;
        }
    }
}