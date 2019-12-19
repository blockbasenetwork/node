using System.Collections.Generic;

namespace BlockBase.DataProxy.Encryption
{
    //TODO - this key store is a local in memory implementation of key storage that should be substituted by another implementation in the future
    public class SecretStore : ISecretStore
    {
        private Dictionary<string, byte[]> _secretStoreDict = new Dictionary<string, byte[]>();

        public void SetSecret(string secretId, byte[] key)
        {
            if (!_secretStoreDict.ContainsKey(secretId))
                _secretStoreDict.Add(secretId, key);
            else
                _secretStoreDict[secretId] = key;
        }

        public byte[] GetSecret(string secretId)
        {
            if (_secretStoreDict.ContainsKey(secretId)) return _secretStoreDict[secretId];
            return null;
        }

        public void RemoveSecret(string secretId)
        {
            if (_secretStoreDict.ContainsKey(secretId))
                _secretStoreDict.Remove(secretId);
        }
    }
}