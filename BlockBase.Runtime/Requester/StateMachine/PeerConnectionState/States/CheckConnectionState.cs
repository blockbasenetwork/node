using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
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
    public class CheckConnectionState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private bool _peersConnected;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractStateTable;
        public CheckConnectionState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechain;
            _peerConnectionsHandler = peerConnectionsHandler;
            _peersConnected = true;
        }

        protected override Task<bool> IsWorkDone()
        {
            //Will always do work when in this state
            return Task.FromResult(false);
        }

        protected override async Task DoWork()
        {
            _peersConnected = await _peerConnectionsHandler.ArePeersConnected(_sidechainPool);
            if (!_peersConnected) _delay = TimeSpan.FromSeconds(0);
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
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);

            _contractStateTable = contractState;
            _delay = TimeSpan.FromSeconds(15);
        }
    }
}