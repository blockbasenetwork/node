using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.StateMachine.BlockProductionState.States;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState
{
    public class BlockProductionStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private SidechainPool _sidechainPool;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private ChainBuilder _chainBuilder;
        private NodeConfigurations _nodeConfigurations;
        private string _endPoint;
        private BlockSender _blockSender;
        private long _nextTimeToCheckSmartContract;
        private long _previousTimeToCheck;
        private IMongoDbProducerService _mongoDbProducerService;

        //TODO: change this when client specifies database type (MYSQL, SQL, ...)
        private ISidechainDatabasesManager _sidechainDatabaseManager;

        public BlockProductionStateManager(SidechainPool sidechainPool, NodeConfigurations nodeConfigurations, ILogger logger, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, BlockSender blockSender, ISidechainDatabasesManager sidechainDatabaseManager)
        {
            _logger = logger;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _blockSender = blockSender;
            _sidechainDatabaseManager = sidechainDatabaseManager;
        }

        protected override IState BuildState(string state)
        {
            throw new System.NotImplementedException();
        }
    }
}
