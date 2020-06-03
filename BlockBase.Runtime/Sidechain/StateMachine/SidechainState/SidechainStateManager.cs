using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState
{
    public class SidechainStateManager : IThreadableComponent
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private INetworkService _networkService;
        private IMongoDbProducerService _mongoDbProducerService;
        private ISidechainDatabasesManager _sidechainDatabasesManager;

        private SidechainPool _sidechain;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private BlockRequestsHandler _blockSender;

        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;

        public TaskContainer TaskContainer { get; private set; }
        public TaskContainer BlockProductionTaskContainer { get; private set; }

        

        //TODO rpinto - it will be the state manager that besides coordinating state changes also is responsible to start the connectionchecker
        public SidechainStateManager(SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, ILogger logger, INetworkService networkService, IMongoDbProducerService mongoDbProducerService, IMainchainService mainchainService, ISidechainDatabasesManager sidechainDatabasesManager, BlockRequestsHandler blockSender)
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

        public TaskContainer Start()
        {
            if(TaskContainer != null) TaskContainer.Stop();
            TaskContainer = TaskContainer.Create(async () => await Run());
            TaskContainer.Start();
            return TaskContainer;
        }

        private async Task Run() 
        {
            var currentState = BuildState(typeof(StartState).Name);

            while(true)
            {
                var nextStateName = await currentState.Run();
                currentState = BuildState(nextStateName);

                if(currentState.GetType() == typeof(EndState))
                {
                    await currentState.Run();
                    BlockProductionTaskContainer.Stop();
                    break;
                }

                if(currentState.GetType() == typeof(ProductionState) && BlockProductionTaskContainer == null)
                {
                    var blockProductionStateManager = new BlockProductionStateManager(_logger, _sidechain, _nodeConfigurations, _networkConfigurations, _networkService, _peerConnectionsHandler, _mainchainService, _mongoDbProducerService, _blockSender, _sidechainDatabasesManager);
                    BlockProductionTaskContainer = blockProductionStateManager.Start();
                }
            }
        }

        private AbstractState BuildState(string state)
        {
            if(state == typeof(StartState).Name) return new StartState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(CandidatureState).Name) return new CandidatureState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(SecretTimeState).Name) return new SecretTimeState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(IPSendTimeState).Name) return new IPSendTimeState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations);
            if(state == typeof(IPReceiveState).Name) return new IPReceiveState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(ProductionState).Name) return new ProductionState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if(state == typeof(EndState).Name) return new EndState(_sidechain, _logger);

            return null;
        }
    }
}
