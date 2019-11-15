using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Utils.Crypto
{
    public class Onion
    {
        private const int AES_BLOCK_SIZE = 16;
        private const int AES_KEY_SIZE = 32;
        public enum Layer { RND, DET };

        public byte[] CreateOnion(byte[] plainText, byte[] masterKey, byte[] masterIV, string tableName, string columnName, out byte[] randomIV)
        {
            var deterministicLayer = EncryptDeterministicLayer(plainText, masterKey, masterIV, tableName, columnName);

            var random = new SecureRandom();
            randomIV = new byte[AES_BLOCK_SIZE];
            random.NextBytes(randomIV);

            var location = tableName + columnName + Layer.RND.ToString(); // detLayerKey = AESm(sha256(tableName,columnName,layer));
            var locationInBytes = Encoding.ASCII.GetBytes(location);
            var hash = Utils.SHA256(locationInBytes);
            var randomLayerKey = AES256.EncryptWithECB(hash, masterKey);

            //// AESk(detLayer,randomIV)
            return AES256.EncryptWithCBC(deterministicLayer, randomLayerKey, randomIV);
        }
        public byte[] CreateJoinOnion(byte[] plainText, byte[] masterKey, byte[] masterIV, string tableName, string columnName, ECPoint pointP, byte[] baseKey, byte[] randomIV)
        {
            var joinEC = new JoinEC(baseKey,masterKey,tableName,columnName,plainText,pointP);
            var joinToken = joinEC.Encrypt();

            var location = tableName + columnName + Layer.RND.ToString(); // detLayerKey = AESm(sha256(tableName,columnName,layer));
            var locationInBytes = Encoding.ASCII.GetBytes(location);
            var hash = Utils.SHA256(locationInBytes);
            var randomLayerKey = AES256.EncryptWithECB(hash, masterKey);

            //// AESk(detLayer,randomIV)
            return AES256.EncryptWithCBC(joinToken, randomLayerKey, randomIV);
        }
        public byte[] GetJoinToken(byte[] cipherText, byte[] masterKey, byte[] masterIV, string tableName, string columnName, byte[] randomIV)
        {
            var location = tableName + columnName + Layer.RND.ToString();
            var locationInBytes = Encoding.ASCII.GetBytes(location);
            var hash = Utils.SHA256(locationInBytes);
            var randomLayerKey = AES256.EncryptWithECB(hash, masterKey);

            return AES256.DecryptWithCBC(cipherText, randomLayerKey, randomIV);
        }
        public byte[] DecryptRandomLayer(byte[] cipherText, byte[] masterKey, string tableName, string columnName,byte[] randomIV)
        {
            var location = tableName + columnName + Layer.RND.ToString(); // detLayerKey = AESm(sha256(tableName,columnName,layer));
            var locationInBytes = Encoding.ASCII.GetBytes(location);
            var hash = Utils.SHA256(locationInBytes);
            var randomLayerKey = AES256.EncryptWithECB(hash, masterKey);

            return AES256.DecryptWithCBC(cipherText, randomLayerKey, randomIV);
        }
        public byte[] EncryptDeterministicLayer(byte[] plainText, byte[] masterKey, byte[] masterIV, string tableName, string columnName)
        {
            string location = tableName + columnName + Layer.DET.ToString(); // detLayerKey = AESm(sha1(tableName,columnName,layer));
            byte[] locationInBytes = Encoding.ASCII.GetBytes(location);
            byte[] hash = Utils.SHA256(locationInBytes);

            var deterministicLayerKey = AES256.EncryptWithECB(hash, masterKey); // _deterministicIV = AESm(sha1(tableName,columnName,layer,masterIV));
            var LocationAndIVArray = Utils.ConcatenateByteArray(locationInBytes, masterIV);
            var deterministicIV = Utils.Sha1AndResize(LocationAndIVArray, AES_BLOCK_SIZE);

            return AES256.EncryptWithCBC(plainText, deterministicLayerKey, deterministicIV);
        }
        public byte[] DecryptDeterministicLayer(byte[] cipherText, byte[] masterKey, byte[] masterIV, string tableName, string columnName)
        {
            var location = tableName + columnName + Layer.DET.ToString(); // detLayerKey = AESm(sha1(tableName,columnName,layer));
            var locationInBytes = Encoding.ASCII.GetBytes(location);
            var hash = Utils.SHA256(locationInBytes);
            var deterministicLayerKey = AES256.EncryptWithECB(hash, masterKey);

            // deterministicIV = AESm(sha1(tableName,columnName,layer,masterIV));
            var LocationAndIVArray = Utils.ConcatenateByteArray(locationInBytes, masterIV);
            var deterministicIV = Utils.Sha1AndResize(LocationAndIVArray, AES_BLOCK_SIZE);
            
            return AES256.DecryptWithCBC(cipherText, deterministicLayerKey, deterministicIV);
        }
    }
}
