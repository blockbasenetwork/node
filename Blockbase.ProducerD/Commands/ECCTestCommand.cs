using System.Text;
using System.Threading.Tasks;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils.Crypto;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Blockbase.ProducerD.Commands
{
    public class ECCTestCommand : IHelperCommand
    {

        private ILogger _logger;

        public ECCTestCommand(ILogger logger)
        {
            _logger = logger;
            
        }
        public async Task ExecuteAsync()
        {
            var msg = "127.0.0.1:4444";
            var msgBytes = Encoding.UTF8.GetBytes(msg);
            
            var hash = HashHelper.Sha256Data(msgBytes);
            var producer1PrivateKey = "5JonLTVP8UTaaatLtGeUJxcrgkZTNjSg2CXJeqoTbf5RhSbXQb3";
            var producer1PublicKey = "EOS8V3duCDE3Ha8WZC4oBgrbmWC6nAUe4r8xnSPWiEsdge34HLJFj";
            var signature = SignatureHelper.SignHash(producer1PrivateKey, hash);
            var verify = SignatureHelper.VerifySignature(producer1PublicKey, signature, hash);
            _logger.LogInformation("Signature verification: " + verify);


            var producer2PrivateKey = "5JFYkwp1dnp4hpM8JBtXzDLB6QFYZBaRdw34DzqsUcHSJf3Gjhb";
            var producer2PublicKey = "EOS8mTkZPggFwMfG7B1PxGyWiNGu7M53p2jNNhA48jn5Nubd2wAjp";

            var encryptedData = AssymetricEncryptionHelper.EncryptData(producer2PublicKey, producer1PrivateKey, msgBytes);
            var decryptedData = AssymetricEncryptionHelper.DecryptData(producer1PublicKey, producer2PrivateKey, encryptedData);
            _logger.LogDebug("Msg received: " + Encoding.UTF8.GetString(decryptedData));
            _logger.LogInformation("Msg the same: " + msgBytes.SequenceEqual(decryptedData));


        }

        public string GetCommandHelp()
        {
            return "st";
        }

        public bool TryParseCommand(string commandStr)
        {
           return "st" == commandStr;
        }
    }
}