using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlockBase.Utils.Crypto
{
    public static class AES256
    {

        public static byte[] EncryptWithCBC(byte[] data, byte[] encryptionKey, byte[] initializeVector)
        {
            return CreateCBCProvider(encryptionKey, initializeVector).CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }
        public static byte[] DecryptWithCBC(byte[] data, byte[] encryptionKey, byte[] initializeVector)
        {
            return CreateCBCProvider(encryptionKey, initializeVector).CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
        }

        private static AesCryptoServiceProvider CreateCBCProvider(byte[] key, byte[] ivStringBytes)
        {
            return new AesCryptoServiceProvider
            {
                KeySize = 256,
                BlockSize = 128,
                Key = key,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                IV = ivStringBytes
            };
        }
        private static AesCryptoServiceProvider CreateECBProvider(byte[] key)
        {
            return new AesCryptoServiceProvider
            {
                KeySize = 256,
                BlockSize = 128,
                Key = key,
                Padding = PaddingMode.None,
                Mode = CipherMode.ECB
            };
        }
        public static byte[] EncryptWithECB(byte[] data, byte[] encryptionKey)
        {
            return CreateECBProvider(encryptionKey).CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }
        public static byte[] DecryptWithECB(byte[] data, byte[] encryptionKey)
        {
            return CreateECBProvider(encryptionKey).CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }
    }
}
