using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Agreement.Kdf;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace BlockBase.Utils.Crypto
{
    public static class AssymetricEncryptionHelper
    {

        private static string SYMMETRIC_ALGORITHM_NAME = "AES";
        private static string SYMMETRIC_CIPHER_NAME = "AES/ECB/PKCS7PADDING";

        public static byte[] EncryptData(string receiverPublicKeyString, string senderPrivateKeyString, byte[] data)
        {
            var receiverPublicKeyParameters = EosKeyHelper.GetECPublicKeyParametersFromString(receiverPublicKeyString);
            var senderPrivateKeyParameters = EosKeyHelper.GetECPrivateKeyParametersFromString(senderPrivateKeyString);
            var sharedSecret = GetSharedSecretValue(receiverPublicKeyParameters, senderPrivateKeyParameters);
            var symmetricKey = DeriveSymmetricKeyFromSharedSecret(sharedSecret);
            // var aes = new AES256();
            // return aes.EncryptWithECB(data, symmetricKey);
            byte[] output = null;
            try
            {
                KeyParameter keyparam = ParameterUtilities.CreateKeyParameter(SYMMETRIC_ALGORITHM_NAME, symmetricKey);
                IBufferedCipher cipher = CipherUtilities.GetCipher(SYMMETRIC_CIPHER_NAME);
                cipher.Init(true, keyparam);
                try
                {
                    output = cipher.DoFinal(data);
                    return output;
                }
                catch (System.Exception ex)
                {
                    throw new CryptoException("Invalid Data. Throwed exception: " + ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return output;
        }

        public static byte[] DecryptData(string senderPublicKeyString, string receiverPrivateKeyString, byte[] cipherData)
        {
            var senderPublicKeyParameters = EosKeyHelper.GetECPublicKeyParametersFromString(senderPublicKeyString);
            var receiverPrivateKeyParameters = EosKeyHelper.GetECPrivateKeyParametersFromString(receiverPrivateKeyString);
            var sharedSecret = GetSharedSecretValue(senderPublicKeyParameters, receiverPrivateKeyParameters);
            var symmetricKey = DeriveSymmetricKeyFromSharedSecret(sharedSecret);
            // var aes = new AES256();
            // return aes.DecryptWithECB(cipherData, symmetricKey);

            byte[] output = null;
            try
            {
                KeyParameter keyparam = ParameterUtilities.CreateKeyParameter(SYMMETRIC_ALGORITHM_NAME, symmetricKey);
                IBufferedCipher cipher = CipherUtilities.GetCipher(SYMMETRIC_CIPHER_NAME);
                cipher.Init(false, keyparam);
                try
                {
                    output = cipher.DoFinal(cipherData);

                }
                catch (System.Exception ex)
                {
                    throw new CryptoException("Invalid Data. Exception thrown: " + ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return output;
        }

        public static byte[] GetSharedSecretValue(ECPublicKeyParameters publicKeyParameters, ECPrivateKeyParameters privateKeyParameters)
        {
            ECDHCBasicAgreement eLacAgreement = new ECDHCBasicAgreement();
            eLacAgreement.Init(privateKeyParameters);
            BigInteger eLA = eLacAgreement.CalculateAgreement(publicKeyParameters);
            return eLA.ToByteArray();
        }

        public static byte[] DeriveSymmetricKeyFromSharedSecret(byte[] sharedSecret)
        {
            Org.BouncyCastle.Crypto.Agreement.Kdf.ECDHKekGenerator egH = new ECDHKekGenerator(DigestUtilities.GetDigest("SHA256"));
            egH.Init(new DHKdfParameters(NistObjectIdentifiers.Aes, sharedSecret.Length, sharedSecret));
            byte[] symmetricKey = new byte[DigestUtilities.GetDigest("SHA256").GetDigestSize()];
            egH.GenerateBytes(symmetricKey, 0, symmetricKey.Length);

            return symmetricKey;
        }


    }
}
