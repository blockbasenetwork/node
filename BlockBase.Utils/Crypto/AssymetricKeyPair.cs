using System.Security.Cryptography;

namespace BlockBase.Utils.Crypto
{
    public class AssymetricKeyPair
    {
        public RSAParameters PrivateKey { get; set; }
        public RSAParameters PublicKey { get; set; }
    }
}