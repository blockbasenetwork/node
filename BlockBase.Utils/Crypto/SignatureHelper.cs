using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cryptography.ECDSA;
using EosSharp.Core.Helpers;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Asn1.Ocsp;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1;
using System.Text.RegularExpressions;

namespace BlockBase.Utils.Crypto
{
    public static class SignatureHelper
    {
        private static readonly byte[] KEY_TYPE_BYTES = Encoding.UTF8.GetBytes("K1");
        private static readonly int CHECKSUM_LENGHT = 4;
        
        private static readonly string SIG_PREFIX = "SIG_K1_";

        
        public static string SignHash(string privateKeyString, byte[] hash)
        {
            try
            {

                ECPrivateKeyParameters privateKeyParameters = EosKeyHelper.GetECPrivateKeyParametersFromString(privateKeyString);
                ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
                signer.Init(true, privateKeyParameters);
                signer.BlockUpdate(hash, 0, hash.Length);
                byte[] sigBytes = signer.GenerateSignature();

                var check = new List<byte[]>() { sigBytes, KEY_TYPE_BYTES };
                var checksum = Ripemd160Manager.GetHash(SerializationHelper.Combine(check)).Take(CHECKSUM_LENGHT).ToArray();
                var signAndChecksum = new List<byte[]>() { sigBytes, checksum };
                var finalSig = SIG_PREFIX + Base58.Encode(SerializationHelper.Combine(signAndChecksum));

                return finalSig;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Signing Failed: " + exc.ToString());
                return null;
            }
        }

        public static bool VerifySignature(string publicKeyString, string signature, byte[] hash)
        {
            try
            {
                byte[] sigBytes = GetSignatureBytesWithoutCheckSum(signature);
                var publicKeyParameters = EosKeyHelper.GetECPublicKeyParametersFromString(publicKeyString);
                ISigner signer = SignerUtilities.GetSigner("NONEwithECDSA");
                signer.Init(false, publicKeyParameters);
                signer.BlockUpdate(hash, 0, hash.Length);
                return signer.VerifySignature(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Verification failed with the error: " + exc.ToString());
                return false;
            }
        }

        private static byte[] GetSignatureBytesWithoutCheckSum(string signature)
        {
            Regex regex = new Regex(@"\bSIG_K1_\S*");
            Match match = regex.Match(signature);
            if (!match.Success) throw new FormatException("Invalid Signature prefix.");

            signature = signature.Substring(SIG_PREFIX.Length);

            var signatureBytesWithChecksum = Base58.Decode(signature);
            var receivedChecksum = signatureBytesWithChecksum.TakeLast(CHECKSUM_LENGHT).ToArray();
            var signatureWithoutChecksum = signatureBytesWithChecksum.Take(signatureBytesWithChecksum.Length-CHECKSUM_LENGHT).ToArray();

            var check = new List<byte[]>() { signatureWithoutChecksum, KEY_TYPE_BYTES };
            var checksum = Ripemd160Manager.GetHash(SerializationHelper.Combine(check)).Take(CHECKSUM_LENGHT).ToArray();

            if (!receivedChecksum.SequenceEqual(checksum)) throw new FormatException("Invalid Signature checksum.");

            return signatureWithoutChecksum;
        }

    }
}