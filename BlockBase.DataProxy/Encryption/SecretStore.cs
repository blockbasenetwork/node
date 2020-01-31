using System;
using System.Collections.Generic;
using System.IO;
using Wiry.Base32;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BlockBase.DataProxy.Encryption
{
    //TODO - this key store is a local in memory implementation of key storage that should be substituted by another implementation in the future
    public class SecretStore : ISecretStore
    {
        private Dictionary<string, byte[]> _secretStoreDict = new Dictionary<string, byte[]>();
        private static readonly string keysFileName = "keys.txt";
        private ILogger _logger;

        public SecretStore(ILogger logger)
        {
            _logger = logger;
            LoadSecrets();
        }

        public void SetSecret(string secretId, byte[] key)
        {
            if(_secretStoreDict.ContainsKey(secretId) && _secretStoreDict[secretId].SequenceEqual(key)) return;
            
            if (!_secretStoreDict.ContainsKey(secretId))
                _secretStoreDict.Add(secretId, key);

            else
            {
                _secretStoreDict[secretId] = key;
                RemoveSecret(secretId);
            }
            using (StreamWriter file = new StreamWriter(keysFileName, true))
            {
                file.WriteLine(secretId + ":" + Base32Encoding.ZBase32.GetString(key));
            }
        }

        public void LoadSecrets()
        {
            try
            {
                using (StreamReader sr = new StreamReader(keysFileName))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var idKey = line.Split(":");
                        _secretStoreDict.Add(idKey[0], Base32Encoding.ZBase32.ToBytes(idKey[1]));
                    }
                }
            }
            catch (Exception)
            {
                _logger.LogError("Could not read/write in file.");
            }
        }

        public byte[] GetSecret(string secretId)
        {
            if (_secretStoreDict.ContainsKey(secretId)) return _secretStoreDict[secretId];
            return null;
        }

        public void RemoveSecret(string secretId)
        {
            try
            {
            if (_secretStoreDict.ContainsKey(secretId))
            {
                _secretStoreDict.Remove(secretId);
                string line = null;

                using (var reader = new StreamReader(keysFileName))
                {
                    using (StreamWriter writer = new StreamWriter(keysFileName))
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains(secretId))
                                continue;

                            writer.WriteLine(line);
                        }
                    }
                }
            }
            }
            catch(Exception)
            {
                _logger.LogError("Could not read/write in file.");
            }
        }
    }
}