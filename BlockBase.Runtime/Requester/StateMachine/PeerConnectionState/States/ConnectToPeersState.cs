using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionsState.States
{
    public class ConnectToPeersState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private List<ProducerInTable> _producers;
        private List<IPAddressTable> _ipAddresses;
        private SidechainPool _sidechainPool;
        public ConnectToPeersState(ref SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainPool = sidechain;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task<bool> IsWorkDone()
        {
            throw new NotImplementedException();
        }

        protected override async Task DoWork()
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            throw new NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            throw new NotImplementedException();
        }

        protected override async Task UpdateStatus() 
        {
            throw new NotImplementedException();
        }

        // private async Task ConnectToProducers()
        // {
        //     var ipAddresses = await GetProducersIPs();
        //     await _peerConnectionsHandler.ConnectToProducers(ipAddresses);
        // }

        // private async Task<IDictionary<string, IPEndPoint>> GetProducersIPs()
        // {
        //     var ipAddressesTables = await _mainchainService.RetrieveIPAddresses(_sidechain.ClientAccountName);

        //     var decryptedProducerIPs = new Dictionary<string, IPEndPoint>();
        //     foreach (var table in ipAddressesTables)
        //     {
        //         var producer = table.Key;
        //         var producerPublicKey = table.PublicKey;
        //         //TODO rpinto - why a list of IPs and not only one?
        //         var encryptedIp = table.EncryptedIPs?.LastOrDefault();
        //         if (encryptedIp == null) continue;

        //         try
        //         {
        //             var decryptedIp = AssymetricEncryption.DecryptIP(encryptedIp, _nodeConfigurations.ActivePrivateKey, producerPublicKey);
        //             decryptedProducerIPs.Add(producer, decryptedIp);
        //         }
        //         catch
        //         {
        //             _logger.LogWarning($"Unable to decrypt IP from producer: {producer}.");
        //         }
        //     }
        //     return decryptedProducerIPs;
        // }
    }

}