using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils
{
    public static class BaseConverterHelper
    {
        private static List<Char> _printableChars;
        private static object Locker = new object();
        private static List<Char> PrintableChars
        {
            get
            {
                lock(Locker)
                {
                    if(_printableChars == null)
                    {
                        _printableChars = new List<char>();
                        for (int i = char.MinValue; i <= char.MaxValue; i++)
                        {
                            char c = Convert.ToChar(i);
                            if (char.IsLetterOrDigit(c))
                            {
                                _printableChars.Add(c);
                            }
                        }
                    }

                    return _printableChars;
                }
            }
        }

        public static string ConvertToBigBaseFromString(string str)
        {
            return ConvertToBigBaseFromBytes(Encoding.Unicode.GetBytes(str));
        }

        public static string ConvertFromBigBaseToString(string str)
        {
            return Encoding.Unicode.GetString(ConvertFromBigBaseToBytes(str));
        }

        public static string ConvertToBigBaseFromBytes(byte[] data)
        {
            // Decode byte[] to BigInteger
            BigInteger intData = 0;
            for (int i = 0; i < data.Length; i++)
            {
                intData = intData * 256 + data[i];
            }

            return ConvertToBigBase(intData);
        }

        public static byte[] ConvertFromBigBaseToBytes(string bigBase)
        {
            var intData = ConvertFromBigBase(bigBase);

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading '1' characters
            int leadingZeroCount = bigBase.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros =
                intData.ToByteArray()
                .Reverse()// to big endian
                .SkipWhile(b => b == 0);//strip sign byte
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
            return result;
        }

        public static string ConvertToBigBase(BigInteger bigInteger)
        {
            string result = "";
            while (bigInteger > 0)
            {
                int remainder = (int)(bigInteger % PrintableChars.Count);
                bigInteger /= PrintableChars.Count;

                result = PrintableChars[remainder] + result;
            }
            return result;
        }

        public static BigInteger ConvertFromBigBase(string bigBase)
        {
            BigInteger intData = 0;
            for (int i = 0; i < bigBase.Length; i++)
            {
                int digit = PrintableChars.IndexOf(bigBase[i]); //Slow
                if (digit < 0)
                    throw new FormatException(string.Format("Invalid Bigbase character '{0}' at position {1}", bigBase[i], i));
                intData = intData * 58 + digit;
            }

            return intData;
        }
    }
}
