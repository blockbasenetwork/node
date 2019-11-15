using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Crypto.Ope
{
    public class PolynomialTerm
    {
        public int Variable { get; set; }
        public int Degree { get; set; }
        public bool IsPositive { get; set; }

        public PolynomialTerm() { }

        public PolynomialTerm(bool isPositive, int variable, int degree)
        {
            IsPositive = isPositive;
            Variable = variable;
            Degree = degree;
        }

        public BigInteger Calculate(long value)
        {
            var bigValue = new BigInteger(value);
            var bigVariable = new BigInteger(Variable);

            var result = bigVariable * BigInteger.Pow(bigValue, Degree);
            if (IsPositive) return result;
            else return BigInteger.MinusOne * result;
        }

        public BigInteger Calculate(BigInteger value)
        {
            var bigVariable = new BigInteger(Variable);
            //var result = BigInteger.Pow(value, Degree) / Degree;
            //var result = BigInteger.Pow(value, Degree) / bigVariable;
            var result = bigVariable * BigInteger.Pow(value, Degree);
            if (IsPositive) return result;
            else return BigInteger.MinusOne * result;
        }
    }

    public class Polynomial
    {
        public IList<PolynomialTerm> Terms { get; set; }

        public Polynomial()
        {
            Terms = new List<PolynomialTerm>();
        }

        public Polynomial(IEnumerable<PolynomialTerm> list)
        {
            Terms = list.ToList();
        }

        public BigInteger Calculate(long value)
        {
            var result = new BigInteger(0);
            foreach (var term in Terms)
                result += term.Calculate(value);

            return result;
        }

        public BigInteger Calculate(BigInteger value)
        {
            var result = new BigInteger(0);
            foreach (var term in Terms)
                result += term.Calculate(value);

            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var term in Terms.OrderByDescending(t => t.Degree))
            {
                var sign = term.IsPositive ? "+" : "-";
                builder.Append($"{sign} {term.Variable}X^{term.Degree} ");
            }
            return builder.ToString();

        }
    }

    public class OrderPreservingHashingConfig
    {
        public string EncodedMaximumNumHash { get; set; }
        public string Divisor { get; set; }

        public Polynomial Polynomial { get; set; }

        public BigInteger BaseDeviationValue { get; set; }

        public int BaseDeviationShift { get; set; }

        public int BaseSectionSize { get; set; }

        private OrderPreservingHashingConfig() { }

        public static OrderPreservingHashingConfig Generate(string password, int maxNumSize)
        {
            //make the password much larger to transform the polynomial
            //var tempPassword = password;
            //for(int i = 0; i < 10; i++)
            //{
            //    password += tempPassword;
            //}


            var opeConfiguration = new OrderPreservingHashingConfig();

            //generate a polynomial
            opeConfiguration.Polynomial = GeneratePolynomial(password);

            ///**********************************************************
            ///FIND A LARGE BASE VALUE TO DEVIATE ALL VALUES TO CALCULATE
            ///**********************************************************

            var hexPasswordData = EncodeToHex(password).ToList();

            BigInteger baseDeviationValue = new BigInteger(0);
            for (int i = 0; i < hexPasswordData.Count; i++)
            {
                //find a number larger than the maximum num size
                baseDeviationValue += BigInteger.Pow(baseDeviationValue + hexPasswordData[i], hexPasswordData[i]);
                if (baseDeviationValue.ToString().Length > maxNumSize + 2) break;
            }

            //reduce the size of the passwordInt to two magnitudes larger that maxNumSize
            baseDeviationValue /= BigInteger.Pow(10, baseDeviationValue.ToString().Length - (maxNumSize + 2));

            //choose a base shift that all values will have
            var shift = baseDeviationValue.ToString().Length - maxNumSize;

            opeConfiguration.BaseDeviationValue = baseDeviationValue;
            opeConfiguration.BaseDeviationShift = shift;

            ///**************************************************************************************************
            ///FIND A BASE MODULUS THAT WILL DETERMINE THE SIZE OF THE SECTION OF THE POLYNOMIAL THAT WILL REPEAT
            ///**************************************************************************************************

            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
            {
                //I don't know why I chose these values
                //var minSize = 4001;
                //var maxSize = 9001;
                var minSize = 3;
                var maxSize = 5;

                var bytes = new byte[1];
                generator.GetBytes(bytes);

                var sectionSize = minSize + bytes[0] % (maxSize - minSize);

                opeConfiguration.BaseSectionSize = sectionSize;

            }


            ///**************************************************************
            ///FIND HASH OF MAXIMUM NUMBER, AND BIGGEST DIVISOR FOR EVERY HASH
            ///**************************************************************

            var maximumNumber = BigInteger.Pow(10, maxNumSize) - 1;
            BigInteger maxNumberHash = new BigInteger(0);
            maximumNumber = OrderPreservingEncryptionHelper.CalculateModulusOfNumberAndBaseNumberHash(maximumNumber, out maxNumberHash, opeConfiguration);
            var maximumNumberHash = OrderPreservingEncryptionHelper.TransformValue(maximumNumber, opeConfiguration) + maxNumberHash;

            //find the smallest difference between two consecutive numbers
            var zeroHash = OrderPreservingEncryptionHelper.TransformValue(0, opeConfiguration);
            var oneHash = OrderPreservingEncryptionHelper.TransformValue(1, opeConfiguration);
            var subDifference = oneHash - zeroHash;
            //find biggest divisor for every hash that doesnt' change order - should be 10^subDifference-1, but this resulted in very close values, where no randomness could be added
            var divisor = BigInteger.Pow(10, subDifference.ToString().Length - 3); // I PUT THE DIFFERENCE TO 3 IN ORDER TO HAVE SOME SPACE BETWEEN  SUBSEQUENT NUMBERS

            //reduce maximumNumberHash according to divisor
            maximumNumberHash /= divisor;
            //reduce maximumNumberHash by encoding it to a very large base
            var encodedMaximumNumberHash = BaseConverterHelper.ConvertToBigBase(maximumNumberHash);

            opeConfiguration.EncodedMaximumNumHash = encodedMaximumNumberHash;
            opeConfiguration.Divisor = divisor.ToString();


            return opeConfiguration;

        }

        private static Polynomial GeneratePolynomial(string password)
        {
            ///*******************************************
            ///GENERATE A POLYNOMIAL BASED ON THE PASSWORD
            ///*******************************************

            //get hex characters from every password char
            var hexPassword = EncodeToHex(password);

            var hexPasswordData = hexPassword.Select(c => HexToInt(c)).ToList();

            var terms = new List<PolynomialTerm>();
            //construct list of polynomial terms
            for (int i = 0; i < hexPasswordData.Count; i++)
            {
                terms.Add(new PolynomialTerm(true, i + 1, hexPasswordData[i] * 2));
            }

            terms = SelectTerms(terms);
            return new Polynomial(terms);
        }

        private static List<PolynomialTerm> SelectTerms(IList<PolynomialTerm> terms)
        {
            var filteredTerms = terms.OrderByDescending(t => t.Degree).GroupBy(g => g.Degree).Select(g => g.First()).ToList();

            //select numTermsToSelect 
            var selectedTerms = filteredTerms.ToList();
            //make every odd term negative - this should yield always positive values
            for (int i = 0; i < selectedTerms.Count; i++)
            {
                if (i % 2 != 0) selectedTerms[i].IsPositive = false;
            }

            return selectedTerms;
        }


        private static string EncodeToHex(string str)
        {
            var encoding = System.Text.Encoding.UTF8;
            var hexBuilder = new StringBuilder();
            foreach (var @byte in encoding.GetBytes(str.ToCharArray()))
            {
                hexBuilder.Append(String.Format("{0:X2}", @byte));
            }
            return hexBuilder.ToString();
        }

        private static int HexToInt(char hexChar)
        {
            switch (hexChar)
            {
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                default: return int.Parse(hexChar.ToString());
            }
        }
    }
}
