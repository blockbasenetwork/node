using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Requester.StateMachine.PeerConnectionState.States;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionState
{
    public class PeerConnectionStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private SidechainPool _sidechain;
        private PeerConnectionsHandler _peerConnectionsHandler;

        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        
        public PeerConnectionStateManager(
            SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler, 
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, ILogger logger, 
             IMainchainService mainchainService):base(logger)
        {
            _sidechain = sidechain;
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _peerConnectionsHandler = peerConnectionsHandler;

        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(NextStateRouter).Name) return new NextStateRouter(_logger, _mainchainService, _nodeConfigurations,_peerConnectionsHandler);
            if(state == typeof(ConnectToPeersState).Name) return new ConnectToPeersState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations,_peerConnectionsHandler);
            if(state == typeof(CheckConnectionState).Name) return new CheckConnectionState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _peerConnectionsHandler);
            if(state == typeof(EndState).Name) return new EndState(_logger);
            if (state == typeof(WaitForEndConfirmationState).Name) return new WaitForEndConfirmationState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            throw new System.NotImplementedException();
        }
    }
}
