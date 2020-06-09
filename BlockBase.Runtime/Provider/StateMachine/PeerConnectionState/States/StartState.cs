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

namespace BlockBase.Runtime.Provider.StateMachine.PeerConnectionState.States
{
    public class StartState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private SidechainPool _sidechainPool;
        public StartState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechain;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task DoWork()
        {
            _peerConnectionsHandler.AddKnownSidechain(_sidechainPool);
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractStateTable.ProductionTime || _contractStateTable.IPReceiveTime);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_contractStateTable.ProductionTime || _contractStateTable.IPReceiveTime, typeof(ConnectToPeersState).Name));
        }

        protected override async Task UpdateStatus() 
        {
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);

            _contractStateTable = contractState;
        }
    }
}