using System.Net;
using System.Text;

namespace BlockBase.Utils.Crypto
{
    public static class IPEncryption
    {
 
 public static string EncryptIP( string endPoint, string senderPrivateKey, string receiverPublicKey)
        {
            // _logger.LogDebug($"Receiver public key {receiverPublicKey}, Endpoint {EndPoint}");
            var ipBytes = Encoding.UTF8.GetBytes(endPoint);
            // _logger.LogDebug($"endpoint bytes {HashHelper.ByteArrayToFormattedHexaString(ipBytes)}");
            var encryptedIP = AssymetricEncryptionHelper.EncryptData(receiverPublicKey, senderPrivateKey, ipBytes);
            // _logger.LogDebug($"encryptedIP {HashHelper.ByteArrayToFormattedHexaString(encryptedIP)}");
            return HashHelper.ByteArrayToFormattedHexaString(encryptedIP);
        }

        public static IPEndPoint DecryptIP(string encryptedIP, string receiverPrivateKey, string senderPublicKey)
        {
            // _logger.LogDebug($"Sender public key {senderPublicKey}, Encrypted IP {EndPoint}");
            var encryptedIPBytes = HashHelper.FormattedHexaStringToByteArray(encryptedIP);
            // _logger.LogDebug($"encryptedIP {HashHelper.ByteArrayToFormattedHexaString(encryptedIPBytes)}");

            var ipBytes = AssymetricEncryptionHelper.DecryptData(senderPublicKey, receiverPrivateKey, encryptedIPBytes);
            // _logger.LogDebug($"endpoint bytes {HashHelper.ByteArrayToFormattedHexaString(ipBytes)}");
            // _logger.LogDebug("Decrypted IP: " + Encoding.UTF8.GetString(ipBytes));

            string[] splitIPEndPoint = Encoding.UTF8.GetString(ipBytes).Split(':');

            if (!IPAddress.TryParse(splitIPEndPoint[0], out var ipAddress)) return null;
            if (!int.TryParse(splitIPEndPoint[1], out var port)) return null;

            return new IPEndPoint(ipAddress, port);
        }
    }
}