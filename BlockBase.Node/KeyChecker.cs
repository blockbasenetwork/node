using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EosSharp.Core.Helpers;
using Cryptography.ECDSA;
using EosSharp.Core.Exceptions;
using EosSharp.Core.Api.v1;

namespace BlockBase.Node
{
    public class KeyChecker
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;

        private ILogger _logger;

        public KeyChecker(ILogger<KeyChecker> logger, IMainchainService mainchainService, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations)
        {
            _mainchainService = mainchainService;

            _nodeConfigurations = nodeConfigurations.Value;
            _networkConfigurations = networkConfigurations.Value;

            _logger = logger;
        }

        public async Task<bool> CheckKeys()
        {
            var privateKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(_nodeConfigurations.ActivePrivateKey);
            var publicKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privateKeyBytes, true));

            if (publicKey != _nodeConfigurations.ActivePublicKey)
            {
                _logger.LogCritical($"The configured key private key {_nodeConfigurations.ActivePrivateKey} doesn't match the configured public key {_nodeConfigurations.ActivePublicKey}");
                return false;
            }

            var account = await TryGetAccount();
            if (account == null)
            {
                _logger.LogCritical($"The configured account {_nodeConfigurations.AccountName} doesn't exist in the configured network");
                return false;
            }

            var keyInAccount = account.permissions.Where(p => p.perm_name == "active").FirstOrDefault().required_auth.keys.Where(k => k.key == publicKey).FirstOrDefault();
            if (keyInAccount == null)
            {
                _logger.LogCritical($"The configured key pair wasn't found in the active permission for account {_nodeConfigurations.AccountName}");
                return false;
            }

            return true;
        }

        public async Task<GetAccountResponse> TryGetAccount()
        {
            try
            {
                return await _mainchainService.GetAccount(_nodeConfigurations.AccountName);
            }
            catch (ApiErrorException)
            {
                return null;
            }
        }
    }
}