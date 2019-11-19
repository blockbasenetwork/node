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
        public const int AES_BLOCK_SIZE = 16;
        private const int AES_KEY_SIZE = 32;
        private static byte[] _salt = { 4, 7, 1, 5, 6, 3, 3, 9 };
        public byte[] MasterKey;

        public KeyAndIVGenerator()
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                MasterKey = new byte[AES_KEY_SIZE];
                rngCryptoServiceProvider.GetBytes(MasterKey);
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
                byte[] randomIV = new byte[AES_BLOCK_SIZE];
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
            return Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(data, MasterKey));
        }

        private byte[] CreateKey(string data)
        {
            var dataCorrectSize = new Rfc2898DeriveBytes(data, _salt).GetBytes(32);
            return CreateKey(dataCorrectSize);
        }

    }
}
