using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Crypto
{
    public static class SymmetricEncryptionHelper
    {
        public static byte[] EncryptData(string password, byte[] data)
       {
            int Rfc2898KeygenIterations = 100;
            int AesKeySizeInBits = 128;

            //the salt should be random...
            byte[] salt = System.Text.Encoding.Unicode.GetBytes(password);
            using (Aes aes = new AesManaged())
            {
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = AesKeySizeInBits;
                int KeyStrengthInBytes = aes.KeySize / 8;
                System.Security.Cryptography.Rfc2898DeriveBytes rfc2898 =
                    new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, Rfc2898KeygenIterations);
                aes.Key = rfc2898.GetBytes(KeyStrengthInBytes);
                aes.IV = rfc2898.GetBytes(KeyStrengthInBytes);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static byte[] DecryptData(string password, byte[] data)
        {
            int Rfc2898KeygenIterations = 100;
            int AesKeySizeInBits = 128;

            //the salt should be random...
            byte[] salt = System.Text.Encoding.Unicode.GetBytes(password);
            using (Aes aes = new AesManaged())
            {
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = AesKeySizeInBits;
                int KeyStrengthInBytes = aes.KeySize / 8;
                System.Security.Cryptography.Rfc2898DeriveBytes rfc2898 =
                    new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, Rfc2898KeygenIterations);
                aes.Key = rfc2898.GetBytes(KeyStrengthInBytes);
                aes.IV = rfc2898.GetBytes(KeyStrengthInBytes);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
