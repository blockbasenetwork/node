using BlockBase.Utils.Crypto.Ope;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Crypto.Ope
{



    public static class OrderPreservingEncryptionHelper
    {
        public static string Hash(long number, OrderPreservingHashingConfig configuration)
        {
            //new attempt to reduce expansion of polynomial
            BigInteger baseNumberHash = new BigInteger(0);
            var bigNumber = CalculateModulusOfNumberAndBaseNumberHash(number, out baseNumberHash, configuration);

            //calculate hash of number
            var numberHash = TransformValue(bigNumber, configuration) + baseNumberHash;
            //calculate hash of next number - it will be useful for deviation the current number whilst preserving order
            var nextNumberHash = TransformValue(bigNumber + 1, configuration) + baseNumberHash;

            //reduce the size of the numbers to the maximum possible
            var divisor = BigInteger.Parse(configuration.Divisor);
            numberHash /= divisor;
            nextNumberHash /= divisor;

            //deviate the number but without surpassing the next
            numberHash = RandomDeviation(numberHash, nextNumberHash);

            //encode the number to further reduce size
            var encodedNumberHash = BaseConverterHelper.ConvertToBigBase(numberHash);

            //add leading zeros to enable correct string comparison
            while (encodedNumberHash.Length < configuration.EncodedMaximumNumHash.Length) encodedNumberHash = "0" + encodedNumberHash;

            return encodedNumberHash;
        }

        internal static BigInteger CalculateModulusOfNumberAndBaseNumberHash(BigInteger number, out BigInteger baseNumberHash, OrderPreservingHashingConfig configuration)
        {
            var modulus = number % configuration.BaseSectionSize;
            var numDivisions = number / configuration.BaseSectionSize;

            baseNumberHash = TransformValue(configuration.BaseSectionSize, configuration);
            baseNumberHash *= numDivisions;


            return modulus;
        }

        private static BigInteger RandomDeviation(BigInteger value, BigInteger maxValueDeviation)
        {
            BigInteger randomDeviation = new BigInteger(0);
            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
            {
                //Console.WriteLine("now:" + value.ToString());
                //Console.WriteLine("nxt:" + maxValueDeviation.ToString());

                var bytes = new byte[4];
                generator.GetBytes(bytes);
                randomDeviation = BigInteger.Abs(new BigInteger(bytes));

                //calculate the difference between both values
                var difference = maxValueDeviation - value;

                //apply the deviation with a modulus of the difference to ensure it maintains the value bounded to the next
                var result = value + randomDeviation % difference;
                if (result >= maxValueDeviation)
                {
                    // Console.WriteLine("Maximum deviation was surpassed");
                }

                //Console.WriteLine("diff:" + difference.ToString());
                //Console.WriteLine("rdev:" + randomDeviation % difference);
                //Console.WriteLine("safe:" + (difference - randomDeviation % difference));

                return result;
            }
        }

        internal static BigInteger TransformValue(BigInteger number, OrderPreservingHashingConfig configuration)
        {
            BigInteger deviationValue = configuration.BaseDeviationValue + number * (long)Math.Pow(10, configuration.BaseDeviationShift);
            return configuration.Polynomial.Calculate(deviationValue);
        }
    }
}
