using BlockBase.Utils.Crypto;
using System.Security.Cryptography;
using System.Text;

namespace BlockBase.DataProxy.Encryption
{
    public static class KeyAndIVGenerator
    {
        public const int AES_BLOCK_SIZE = 16;
        private const int AES_KEY_SIZE = 32;

        public static byte[] CreateRandomKey()
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var key = new byte[AES_KEY_SIZE];
                rngCryptoServiceProvider.GetBytes(key);
                return key;
            }
        }

        public static byte[] CreateDerivateKey(byte[] parentKey, byte[] parentIV)
        {
            return Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(parentIV, parentKey));
        }

        public static byte[] CreateMasterIV(string password)
        {
            return Utils.Crypto.Utils.MD5(Utils.Crypto.Utils.SHA256(Encoding.ASCII.GetBytes(password)));
        }

        public static byte[] CreateRandomIV()
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                byte[] randomIV = new byte[AES_BLOCK_SIZE];
                rngCryptoServiceProvider.GetBytes(randomIV);
                return randomIV;
            }
        }

        private static byte[] CreateKey(byte[] data, string key)
        {
            return Utils.Crypto.Utils.SHA256(AES256.EncryptWithECB(data, Wiry.Base32.Base32Encoding.ZBase32.ToBytes(key)));
        }
    }
}