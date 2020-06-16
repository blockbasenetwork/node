using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionState.States
{
    public class ConnectToPeersState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private List<ProducerInTable> _producers;
        private IDictionary<string, IPEndPoint> _ipAddresses;
        private SidechainPool _sidechainPool;
        public ConnectToPeersState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainPool = sidechain;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task<bool> IsWorkDone()
        {
            var connectedPeersExist = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Any(p => p.ConnectionState == ConnectionStateEnum.Connected);

            return Task.FromResult(connectedPeersExist);
        }

        protected override async Task DoWork()
        {
            await _peerConnectionsHandler.ConnectToProducers(_ipAddresses);
            AddProducersToSidechainPool(_sidechainPool);

        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_producers != null && _ipAddresses != null && _ipAddresses.Any());
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var connectedPeersExist = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Any(p => p.ConnectionState == ConnectionStateEnum.Connected);

            return Task.FromResult((connectedPeersExist, typeof(CheckConnectionState).Name));
        }

        protected override async Task UpdateStatus() 
        {
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _ipAddresses = await GetProducersIPs();
            _delay = TimeSpan.FromSeconds(_networkConfigurations.ConnectionExpirationTimeInSeconds);
        }


        private async Task<IDictionary<string, IPEndPoint>> GetProducersIPs()
        {
            var ipAddressesTables = await _mainchainService.RetrieveIPAddresses(_nodeConfigurations.AccountName);

            var decryptedProducerIPs = new Dictionary<string, IPEndPoint>();
            foreach (var table in ipAddressesTables)
            {
                var producer = table.Key;
                var producerPublicKey = table.PublicKey;
                //TODO rpinto - why a list of IPs and not only one?
                var encryptedIp = table.EncryptedIPs?.LastOrDefault();
                if (encryptedIp == null) continue;

                try
                {
                    var decryptedIp = AssymetricEncryption.DecryptIP(encryptedIp, _nodeConfigurations.ActivePrivateKey, producerPublicKey);
                    decryptedProducerIPs.Add(producer, decryptedIp);
                }
                catch
                {
                    _logger.LogWarning($"Unable to decrypt IP from producer: {producer}.");
                }
            }
            return decryptedProducerIPs;
        }

        private void AddProducersToSidechainPool(SidechainPool sidechainPool)
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();

            var producersInPool = _producers.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    NewlyJoined = false,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    IPEndPoint = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()?.IPEndPoint
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
        }
    }

}