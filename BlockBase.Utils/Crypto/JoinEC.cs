using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Utils.Crypto
{

    public class JoinEC
    {
        public static X9ECParameters secnamecurves = ECNamedCurveTable.GetByName("secp256k1");
        private BigInteger _tokenInNumber;
        private ECPoint _p;
        public ECPoint G { get; set; }
        public BigInteger N { get; set; }
        public BigInteger Key { get; set; }
        private ECPoint _kp;
        private byte[] _baseKey;
        public byte[] Value { get; private set; }
        
        public JoinEC(byte[] baseKey, byte[] masterKey, string tableName, string columnName, byte[] value, ECPoint pointP)
        {
            Value = value;
            ECKeyPairGenerator gen = new ECKeyPairGenerator();

            _p = pointP;
            G = secnamecurves.G;
            N = secnamecurves.N;
            
            _baseKey = baseKey;
            Key = GetJoinKey(masterKey, tableName, columnName);
            _kp = _p.Multiply(Key);
            var KaInverse = Key.ModInverse(N);
        }

        public static BigInteger GetJoinKey(byte[] masterKey, string tableName, string columnName)
        {

            string location = tableName + columnName + "JOIN";
            byte[] locationInBytes = Encoding.ASCII.GetBytes(location);
            byte[] hash = Utils.SHA256(locationInBytes);
       
            var deterministicLayerKey = AES256.EncryptWithECB(hash, masterKey);
            var layerKey = AES256.EncryptWithECB(deterministicLayerKey, masterKey);
            var key = new BigInteger(layerKey);
            key = key.Mod(secnamecurves.N);
            return key;
        }

        public static ECPoint GetRandomPoint()
        {
            SecureRandom secureRandom = new SecureRandom();
            BigInteger x;
            do x = new BigInteger(secnamecurves.N.BitLength, secureRandom);
            while (x.Equals(BigInteger.Zero) || (x.CompareTo(secnamecurves.N) >= 0));
            var curvePoint = secnamecurves.Curve.ImportPoint(secnamecurves.G.Multiply(x));
            return curvePoint;
        }
        //generate a token with 33 bytes
        public byte[] Encrypt()
        {
            byte[] hash;
            using (System.Security.Cryptography.SHA1Managed sha1 = new System.Security.Cryptography.SHA1Managed())
            {
                hash = sha1.ComputeHash(Value);
            }
            var resizeHash128 = new byte[16];
            Array.Copy(hash, resizeHash128, 16);

            byte[] token = AES256.EncryptWithECB(resizeHash128, _baseKey);
           
            _tokenInNumber = new BigInteger(token);
            _tokenInNumber = _tokenInNumber.Mod(N);
            _kp = _kp.Multiply(_tokenInNumber);

            return _kp.GetEncoded(true) ;
        }
        public static BigInteger GetDeltaKey(BigInteger Ka,BigInteger Kb)
        {
            var KaInverse = Ka.ModInverse(secnamecurves.N);
            return KaInverse.Multiply(Kb).Mod(secnamecurves.N);
        }

        public static byte[] Adjust(BigInteger delta,byte[] encoded)
        {
            var point = secnamecurves.Curve.DecodePoint(encoded);
            return point.Multiply(delta).GetEncoded(true);
        }
    }
}
