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

namespace BlockBase.Runtime.Provider.StateMachine.PeerConnectionState.States
{
    public class ConnectToPeersState : ProviderAbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private List<ProducerInTable> _producers;
        private List<IPAddressTable> _ipAddresses;
        private SidechainPool _sidechainPool;
        public ConnectToPeersState(ref SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger, sidechainPool)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainPool = sidechainPool;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task<bool> IsWorkDone()
        {
            var connectedPeersExist = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Any(p => p.ConnectionState == ConnectionStateEnum.Connected);

            return Task.FromResult(connectedPeersExist);
        }

        protected override async Task DoWork()
        {
            _peerConnectionsHandler.AddKnownSidechain(_sidechainPool);

            AddProducersToSidechainPool();
            UpdateIPsInSidechain();
            
            await _peerConnectionsHandler.UpdateConnectedProducersInSidechainPool(_sidechainPool);
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
            _ipAddresses = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);
            
            _delay = TimeSpan.FromSeconds(_networkConfigurations.ConnectionExpirationTimeInSeconds);
        }

        private void AddProducersToSidechainPool()
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

            _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
        }

        private void UpdateIPsInSidechain()
        {
            if (!_ipAddresses.Any() || _ipAddresses.Any(t => !t.EncryptedIPs.Any())) return;
            foreach (var ipAddressTable in _ipAddresses) ipAddressTable.EncryptedIPs.RemoveAt(ipAddressTable.EncryptedIPs.Count - 1);

            int numberOfIpsToUpdate = (int)Math.Ceiling(_sidechainPool.ProducersInPool.Count() / 4.0);
            if (numberOfIpsToUpdate == 0) return;

            var producersInPoolList = _sidechainPool.ProducersInPool.GetEnumerable().ToList();
            if (!producersInPoolList.Any(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)) return;
            var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)).Take(numberOfIpsToUpdate).ToList();

            foreach (var producer in orderedProducersInPool)
            {
                var producerIndex = orderedProducersInPool.IndexOf(producer);
                var producerIps = _ipAddresses.Where(p => p.Key == producer.ProducerInfo.AccountName).FirstOrDefault();
                if (producerIps == null || producer.ProducerInfo.IPEndPoint != null) continue;

                var listEncryptedIPEndPoints = producerIps.EncryptedIPs;
                var encryptedIpEndPoint = listEncryptedIPEndPoints[producerIndex];
                producer.ProducerInfo.IPEndPoint = AssymetricEncryption.DecryptIP(encryptedIpEndPoint, _nodeConfigurations.ActivePrivateKey, producer.ProducerInfo.PublicKey);
            }
        }

    }

}