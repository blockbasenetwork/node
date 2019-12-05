using System.Collections.Generic;

namespace BlockBase.DataProxy.Encryption
{
    //TODO - this key store is a local in memory implementation of key storage that should be substituted by another implementation in the future
    public class KeyStore : IKeyStore
    {
        private Dictionary<string, byte[]> _keyStoreDict = new Dictionary<string, byte[]>();

        public void SetKey(string keyId, byte[] key)
        {
            if (!_keyStoreDict.ContainsKey(keyId))
                _keyStoreDict.Add(keyId, key);
            else
                _keyStoreDict[keyId] = key;
        }

        public byte[] GetKey(string keyId)
        {
            if (_keyStoreDict.ContainsKey(keyId)) return _keyStoreDict[keyId];
            return null;
        }

        public void RemoveKey(string keyId)
        {
            if (_keyStoreDict.ContainsKey(keyId))
                _keyStoreDict.Remove(keyId);
        }
    }
}