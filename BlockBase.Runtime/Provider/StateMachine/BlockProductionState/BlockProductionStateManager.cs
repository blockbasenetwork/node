using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
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
        private NodeConfigurations _nodeConfigurations;

        private NetworkConfigurations _networkConfigurations;
        private BlockRequestsHandler _blockSender;
        private IMongoDbProducerService _mongoDbProducerService;
        TransactionValidationsHandler _transactionValidationsHandler;


        public BlockProductionStateManager(ILogger logger,
            SidechainPool sidechainPool, NodeConfigurations nodeConfigurations,
            NetworkConfigurations networkConfigurations, INetworkService networkService,
            PeerConnectionsHandler peerConnectionsHandler, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, BlockRequestsHandler blockSender,
            TransactionValidationsHandler transactionValidationsHandler) : base(logger)
        {
            _logger = logger;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _mongoDbProducerService = mongoDbProducerService;

            _blockSender = blockSender;
            _transactionValidationsHandler = transactionValidationsHandler;

        }

        protected override IState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_logger, _mainchainService, _nodeConfigurations, _sidechainPool);
            if(state == typeof(SynchronizeNodeState).Name) return new SynchronizeNodeState(_logger, _mainchainService, _mongoDbProducerService, _sidechainPool, _nodeConfigurations, _networkConfigurations, _networkService, _transactionValidationsHandler);
            if(state == typeof(SynchronizeValidatorNodeState).Name) return new SynchronizeValidatorNodeState(_logger, _mainchainService, _mongoDbProducerService, _sidechainPool, _nodeConfigurations, _networkConfigurations, _networkService, _transactionValidationsHandler);
            if(state == typeof(NetworkReactionState).Name) return new NetworkReactionState(_logger, _nodeConfigurations, _mainchainService, _sidechainPool);
            if(state == typeof(ProduceBlockState).Name) return new ProduceBlockState(_logger, _mainchainService, _mongoDbProducerService, _sidechainPool, _nodeConfigurations, _networkConfigurations,  _blockSender);
            if(state == typeof(EndState).Name) return new EndState(_logger);
            throw new System.NotImplementedException();
        }
    }
}
