using System;
using System.Linq;
using System.Text.RegularExpressions;
using Cryptography.ECDSA;
using EosSharp.Core.Helpers;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace BlockBase.Utils.Crypto
{
    public static class EosKeyHelper
    {
        private static readonly int PUBLIC_KEY_LENGHT = 33;
        private static readonly string EOS_PREFIX = "EOS";
        private static readonly int CHECKSUM_LENGHT = 4;
        private static X9ECParameters ecParams = ECNamedCurveTable.GetByName("secp256k1");
        private static ECDomainParameters domainParams = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed());
        private static Org.BouncyCastle.Math.EC.ECCurve curve = ecParams.Curve;


        internal static byte[] GetPublicKeyBytesWithoutCheckSum(string publicKeyString)
        {
            Regex regex = new Regex(@"\bEOS\S*");
            Match match = regex.Match(publicKeyString);
            if (!match.Success) throw new FormatException("Invalid public key prefix.");

            publicKeyString = publicKeyString.Substring(EOS_PREFIX.Length);
            var publicKeyBytesWithChecksum = Base58.Decode(publicKeyString);
            var publicKeyBytesWithoutChecksum = publicKeyBytesWithChecksum.Take(PUBLIC_KEY_LENGHT).ToArray();

            var receivedChecksum = publicKeyBytesWithChecksum.TakeLast(CHECKSUM_LENGHT).ToArray();
            var checksum = Ripemd160Manager.GetHash(publicKeyBytesWithoutChecksum).Take(CHECKSUM_LENGHT).ToArray();

            if (!receivedChecksum.SequenceEqual(checksum)) throw new FormatException("Invalid public key checksum.");

            return publicKeyBytesWithoutChecksum;
        }

        internal static ECPublicKeyParameters GetECPublicKeyParametersFromString(string publicKeyString)
        {
            var publicKeyBytes = GetPublicKeyBytesWithoutCheckSum(publicKeyString);
            Org.BouncyCastle.Math.EC.ECPoint q = curve.DecodePoint(publicKeyBytes);
            return new ECPublicKeyParameters(q, domainParams);
        }

        internal static ECPrivateKeyParameters GetECPrivateKeyParametersFromString(string privateKeyString)
        {
            var privateKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(privateKeyString);
            var d = new BigInteger(1, privateKeyBytes);
            return new ECPrivateKeyParameters(d, domainParams);
        }

         internal static AsymmetricCipherKeyPair GenerateAssymetricKeyPairs()
        {
            var curve = ECNamedCurveTable.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

            var secureRandom = new SecureRandom();
            var keyParams = new ECKeyGenerationParameters(domainParams, secureRandom);

            var generator = new ECKeyPairGenerator("ECDSA");
            generator.Init(keyParams);
            var keyPair = generator.GenerateKeyPair();

            var privateKey = keyPair.Private as ECPrivateKeyParameters;
            var publicKey = keyPair.Public as ECPublicKeyParameters;

            Console.WriteLine($"Private key: {ToHex(privateKey.D.ToByteArrayUnsigned())}");
            Console.WriteLine($"Public key: {ToHex(publicKey.Q.GetEncoded())}");

            return keyPair;
        }

        static string ToHex(byte[] data) => String.Concat(data.Select(x => x.ToString("x2")));

    }

}