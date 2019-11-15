using BlockBase.Utils.Crypto;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    class Encryptor
    {
        private KeyAndIVGenerator _keyAndIVGenerator;

        public Encryptor()
        {
            _keyAndIVGenerator = new KeyAndIVGenerator();
        }

        public string EncryptDatabaseName(string databaseName)
        {
            return EncryptName(databaseName, _keyAndIVGenerator.CreateDatabaseKey());
        }

        public string EncrypTableName(string databaseName, string tableName)
        {
            return EncryptName(tableName, _keyAndIVGenerator.CreateTableKey(databaseName));
        }

        public string EncrypColumnName(string databaseName, string tableName, string columnName)
        {
            return EncryptName(columnName, _keyAndIVGenerator.CreateColumnKey(databaseName, tableName));
        }

        public string DecryptColumnName(string databaseName, string tableName, string encryptedColumnName)
        {
            return DecryptName(encryptedColumnName, _keyAndIVGenerator.CreateColumnKey(databaseName, tableName));
        }

        public string EncryptUniqueValue(string tableName, string columnName, string value)
        {
            return EncryptName(value, _keyAndIVGenerator.CreateValueKey(tableName, columnName));
        }

        public string EncryptNormalValue(string value, string columnName, string tableName, out byte[] randomIV)
        {
            randomIV = _keyAndIVGenerator.CreateRandomIV();
            var valueInBytes = Encoding.ASCII.GetBytes(value);
            return Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(valueInBytes, _keyAndIVGenerator.CreateValueKey(tableName, columnName), randomIV));
        }


        public string EncryptName(string name, byte[] key)
        {
            var nameInBytes = Encoding.ASCII.GetBytes(name);
            return Base32Encoding.ZBase32.GetString(AES256.EncryptWithCBC(nameInBytes, key, _keyAndIVGenerator.CreateMasterIV()));
        }

        public string DecryptName(string encryptedName, byte[] key)
        {
            var encryptedNameInBytes = Base32Encoding.ZBase32.ToBytes(encryptedName);
            return Encoding.ASCII.GetString(AES256.DecryptWithCBC(encryptedNameInBytes, key, _keyAndIVGenerator.CreateMasterIV()));
        }

        public byte[] GetEqualityBucket(string tableName, string columnName, string value, int N)
        {
            var valueBytes = Encoding.ASCII.GetBytes(value);
            var valueKey = _keyAndIVGenerator.CreateValueKey(tableName, columnName);
            var bucket = new BigInteger(Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(valueBytes, valueKey))) % N;
            return bucket.ToByteArray();

        }
        public byte[] GetRangeBucket(string tableName, string columnName, string bucketUpperBoundValue)
        {
            var valueBytes = Encoding.ASCII.GetBytes(bucketUpperBoundValue);
            var valueKey = _keyAndIVGenerator.CreateValueKey(tableName, columnName);
            var bucket = Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(valueBytes, valueKey));
            return bucket;
        }


    }
}
