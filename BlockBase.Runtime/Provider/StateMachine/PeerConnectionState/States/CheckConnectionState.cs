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
    public class CheckConnectionState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private NodeConfigurations _nodeConfigurations;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private List<IPAddressTable> _ipAddresses;
        private bool _peersConnected;
        private bool _hasDoneWorkOnce;

        public CheckConnectionState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler) : base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
            _peerConnectionsHandler = peerConnectionsHandler;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override Task<bool> IsWorkDone()
        {
            //Will always do work when in this state
            return Task.FromResult(false);
        }

        protected override async Task DoWork()
        {
            await _peerConnectionsHandler.ArePeersConnected(_sidechainPool);
            _hasDoneWorkOnce = true;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractStateTable != null && (_contractStateTable.ProductionTime || _contractStateTable.IPReceiveTime) && _producers.Any(c => c.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((!_peersConnected && _hasDoneWorkOnce, typeof(ConnectToPeersState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _ipAddresses = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);

            if (!_producers.Any(c => c.Key == _nodeConfigurations.AccountName)) return;

            UpdateProducersInSidechainPool();
            UpdateIPsInSidechain();

            var producersInPoolList = _sidechainPool.ProducersInPool.GetEnumerable().ToList();
            var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName));
            var numberOfConnections = (int)Math.Ceiling(producersInPoolList.Count / 4.0);
            var producersWhoIAmSupposedToBeConnected = orderedProducersInPool.Take(numberOfConnections).Where(m => m.PeerConnection == null || m.PeerConnection.ConnectionState != ConnectionStateEnum.Connected).ToList();

            if (producersWhoIAmSupposedToBeConnected.Any())
            {
                _peersConnected = false;
                _delay = TimeSpan.FromSeconds(0);
            }
            else
            {
                _peersConnected = true;
                _delay = TimeSpan.FromSeconds(15);
            }
        }

        private void UpdateProducersInSidechainPool()
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
                try
                {
                    var producerIndex = orderedProducersInPool.IndexOf(producer);
                    var producerIps = _ipAddresses.Where(p => p.Key == producer.ProducerInfo.AccountName).FirstOrDefault();
                    if (producerIps == null || producer.ProducerInfo.IPEndPoint != null) continue;

                    var listEncryptedIPEndPoints = producerIps.EncryptedIPs;
                    var encryptedIpEndPoint = listEncryptedIPEndPoints[producerIndex];
                    producer.ProducerInfo.IPEndPoint = AssymetricEncryption.DecryptIP(encryptedIpEndPoint, _nodeConfigurations.ActivePrivateKey, producer.ProducerInfo.PublicKey);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unable to decrypt {producer.ProducerInfo.AccountName}'s IP");
                    _logger.LogDebug($"Error decrypting: {e}");
                }
            }
        }
    }
}