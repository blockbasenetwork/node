using BlockBase.Utils.Crypto;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlockBase.DataProxy.Encryption
{
    class KeyAndIVGenerator_v2
    {

        private static string _password = "qwerty123";
        public const int AES_BLOCK_SIZE = 16;
        private const int AES_KEY_SIZE = 32;
        public byte[] MasterKey;

        public KeyAndIVGenerator_v2()
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


        private byte[] CreateKey(byte[] data, string key)
        {
            return Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(data, Wiry.Base32.Base32Encoding.ZBase32.ToBytes(key)));
        }

        private byte[] CreateKey(string data, string salt, string key)
        {
            var dataCorrectSize = new Rfc2898DeriveBytes(data, Wiry.Base32.Base32Encoding.ZBase32.ToBytes(salt)).GetBytes(32);
            return CreateKey(dataCorrectSize, key);
        }
    }
}
