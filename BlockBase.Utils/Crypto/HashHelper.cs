using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlockBase.Utils.Crypto
{
    public static class HashHelper
    {
        public static byte[] Sha256Data(byte[] data)
        {
            using (var sha256 = new SHA256Managed())
            {
                return sha256.ComputeHash(data);
            }
        }

        public static byte[] Sha512Data(byte[] data)
        {
            using (var sha512 = new SHA512Managed())
            {
                return sha512.ComputeHash(data);
            }
        }

        public static byte[] RipemdData(byte[] data)
        {
            RIPEMD160 ripemd = RIPEMD160Managed.Create();
            return ripemd.ComputeHash(data);
        }

        public static string ByteArrayToFormattedHexaString(byte[] hash)
        {
            var hashString = BitConverter.ToString(hash);
            return hashString.Replace("-", "").ToLower();
        }

        public static byte[] FormattedHexaStringToByteArray(string hash)
        {
            var stringArray = Regex.Split(hash, "(?:(\\G.{2})+)").Where(s => s != string.Empty).ToArray();
            return Array.ConvertAll(stringArray, s => byte.Parse(s, System.Globalization.NumberStyles.HexNumber));
        }
    }
}
