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

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionState.States
{
    public class CheckConnectionState : AbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private List<IPAddressTable> _ipAddresses;
        private bool _peersConnected;

        public CheckConnectionState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler) : base(logger)
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
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractStateTable != null && (_contractStateTable.ProductionTime || _contractStateTable.IPReceiveTime));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((!_peersConnected, typeof(ConnectToPeersState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _ipAddresses = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);

            UpdateProducersInSidechainPool();

            if (_sidechainPool.ProducersInPool.GetEnumerable().Any(p => p.PeerConnection == null || p.PeerConnection?.ConnectionState != ConnectionStateEnum.Connected))
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
    }
}