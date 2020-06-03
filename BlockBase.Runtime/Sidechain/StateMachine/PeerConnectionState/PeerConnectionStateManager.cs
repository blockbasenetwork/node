using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.StateMachine.PeerConnectionState.States;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.PeerConectionState
{
    public class PeerConnectionStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private INetworkService _networkService;
        private IMongoDbProducerService _mongoDbProducerService;
        private ISidechainDatabasesManager _sidechainDatabasesManager;

        private SidechainPool _sidechain;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private BlockSender _blockSender;

        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        
        public PeerConnectionStateManager(SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, ILogger logger, INetworkService networkService, IMongoDbProducerService mongoDbProducerService, IMainchainService mainchainService, ISidechainDatabasesManager sidechainDatabasesManager, BlockSender blockSender)
        {
            _sidechain = sidechain;
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _networkService = networkService;
            _mongoDbProducerService = mongoDbProducerService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _sidechainDatabasesManager = sidechainDatabasesManager;
            _blockSender = blockSender;
        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _peerConnectionsHandler);
            if(state == typeof(ConnectToPeersState).Name) return new ConnectToPeersState(ref _sidechain, _logger, _mainchainService, _nodeConfigurations, _peerConnectionsHandler);
            if(state == typeof(CheckConnectionState).Name) return new CheckConnectionState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _peerConnectionsHandler);
            if(state == typeof(EndState).Name) return new EndState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _peerConnectionsHandler);
            throw new System.NotImplementedException();
        }
    }
}
