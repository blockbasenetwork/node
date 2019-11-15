using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace BlockBase.Utils.Crypto
{
    public static class AddressHelper
    {
        public static byte[] DecodeFromBase58(string s)
        {
            string digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

            // Decode Base58 string to BigInteger
            BigInteger intData = 0;
            for (int i = 0; i < s.Length; i++)
            {
                int digit = digits.IndexOf(s[i]); //Slow
                if (digit < 0)
                    throw new FormatException(string.Format("Invalid Base58 character '{0}' at position {1}", s[i], i));
                intData = intData * 58 + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading '1' characters
            int leadingZeroCount = s.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros =
                intData.ToByteArray()
                .Reverse()// to big endian
                .SkipWhile(b => b == 0);//strip sign byte
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
            return result;
        }

        public static string GenerateAddress(RSAParameters publicKey)
        {
            byte[] publicKeyBytes = publicKey.Exponent.Concat(publicKey.Modulus).ToArray();
            var publicKeySha256Hash = HashHelper.Sha256Data(publicKeyBytes);
            var ripemdHash = HashHelper.RipemdData(publicKeySha256Hash);

            var prefixedHash = new byte[1].Concat(ripemdHash).ToArray();
            var prefixedHashDoubleHash = HashHelper.Sha256Data(HashHelper.Sha256Data(prefixedHash));

            var checksum = prefixedHashDoubleHash.Take(4).ToArray();
            var prefixedHashAndChecksum = prefixedHash.Concat(checksum).ToArray();

            return EncodeToBase58(prefixedHashAndChecksum);
        }

        private static string EncodeToBase58(byte[] data)
        {
            string digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

            // Decode byte[] to BigInteger
            BigInteger intData = 0;
            for (int i = 0; i < data.Length; i++)
            {
                intData = intData * 256 + data[i];
            }

            // Encode BigInteger to Base58 string
            string result = "";
            while (intData > 0)
            {
                int remainder = (int)(intData % 58);
                intData /= 58;
                result = digits[remainder] + result;
            }

            // Append '1' for each leading 0 byte
            for (int i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = '1' + result;
            }
            return result;
        }
    }
}