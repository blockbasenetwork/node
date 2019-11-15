using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Crypto
{
    public class PrivateKeyContainer
    {
        public byte[] D { get; set; }
        public byte[] DP { get; set; }
        public byte[] DQ { get; set; }
        public byte[] Exponent { get; set; }
        public byte[] InverseQ { get; set; }
        public byte[] Modulus { get; set; }
        public byte[] P { get; set; }
        public byte[] Q { get; set; }

        public void ConvertFromRSA(RSAParameters privateKey)
        {
            D = privateKey.D;
            DP = privateKey.DP;
            DQ = privateKey.DQ;
            Exponent = privateKey.Exponent;
            InverseQ = privateKey.InverseQ;
            Modulus = privateKey.Modulus;
            P = privateKey.P;
            Q = privateKey.Q;
        }

        public  RSAParameters ConvertToRSA()
        {
            var privateKey = new RSAParameters();

            privateKey.D = D;
            privateKey.DP = DP;
            privateKey.DQ = DQ;
            privateKey.Exponent = Exponent;
            privateKey.InverseQ = InverseQ;
            privateKey.Modulus = Modulus;
            privateKey.P = P;
            privateKey.Q = Q;

            return privateKey;
        }
    }
}
