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

namespace BlockBase.Runtime.Mainchain.StateMachine.PeerConnectionsState.States
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
    }
}