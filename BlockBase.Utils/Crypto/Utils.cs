using System;
using System.Security.Cryptography;

namespace BlockBase.Utils.Crypto
{
    public class Utils
    {
        public static byte[] Sha1AndResize(byte[] value, int size)
        {
            byte[] hash = SHA1(value);
            var resizeHash128 = new byte[size];
            Array.Copy(hash, resizeHash128, size);
            return resizeHash128;
        }

        public static byte[] MD5(byte[] value)
        {
            byte[] hash;
            using (MD5CryptoServiceProvider CryptoServiceProvider = new MD5CryptoServiceProvider())
            {
                hash = CryptoServiceProvider.ComputeHash(value);
            }
            return hash;
        }

        public static byte[] SHA1(byte[] value)
        {
            byte[] hash;
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                hash = sha1.ComputeHash(value);
            }
            return hash;
        }

        public static byte[] SHA256(byte[] value)
        {
            byte[] hash;
            using (SHA256 sha256 = new SHA256Managed())
            {
                hash = sha256.ComputeHash(value);
            }
            return hash;
        }

        public static byte[] XorByteArray(byte[] a, byte[] b, int size)
        {
            var array1 = a;
            var array2 = b;
            if (a.Length < size)
            {
                array1 = new byte[size];
                System.Buffer.BlockCopy(a, 0, array1, 0, a.Length);
            }
            if (b.Length < size)
            {
                array2 = new byte[size];
                System.Buffer.BlockCopy(b, 0, array2, 0, b.Length);
            }
            byte[] resultBuffer = new byte[size];

            for (int i = 0; i < size; i++)
            {
                resultBuffer[i] = (byte)(array1[i] ^ array2[i]);
            }
            return resultBuffer;
        }

        public static byte[] ConcatenateByteArray(byte[] a1, byte[] a2)
        {
            byte[] rv = new byte[a1.Length + a2.Length];
            System.Buffer.BlockCopy(a1, 0, rv, 0, a1.Length);
            System.Buffer.BlockCopy(a2, 0, rv, a1.Length, a2.Length);
            return rv;
        }

        public static bool AreByteArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }

            return true;
        }
    }
}