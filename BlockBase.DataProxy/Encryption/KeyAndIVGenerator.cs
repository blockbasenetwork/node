using BlockBase.Utils.Crypto;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlockBase.DataProxy.Encryption
{
    class KeyAndIVGenerator
    {

        //TODO: deal with password, let user choose it and save it
        private static string _password = "qwerty123";
        private static byte[] _salt = { 4, 7, 1, 5, 6, 3, 3, 9 };
        private byte[] _masterKey;

        public KeyAndIVGenerator()
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                _masterKey = new byte[32];
                rngCryptoServiceProvider.GetBytes(_masterKey);
            }
            
        }

        public byte[] CreateMasterIV()
        {
            return Utils.Crypto.Utils.MD5(Utils.Crypto.Utils.SHA256(Encoding.ASCII.GetBytes(_password)));
        }

        public byte[] CreateRandomIV()
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                byte[] randomIV = new byte[16];
                rngCryptoServiceProvider.GetBytes(randomIV);
                return randomIV;
            }
        }

        public byte[] CreateDatabaseKey()
        {
            return CreateKey(_password);
        }

        public byte[] CreateTableKey(string databaseName)
        {
            return CreateKey(databaseName);
        }

        public byte[] CreateColumnKey(string databaseName, string tableName)
        {
            return CreateKey(databaseName + tableName);
        }

        public byte[] CreateValueKey(string tableName, string columnName)
        {
            return CreateKey(tableName + columnName);
        }

        private byte[] CreateKey(byte[] data)
        {
            return Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(data, _masterKey));
        }

        private byte[] CreateKey(string data)
        {
            var dataCorrectSize = new Rfc2898DeriveBytes(data, _salt).GetBytes(32);
            return CreateKey(dataCorrectSize);
        }

    }
}
