using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Requester.StateMachine.PeerConnectionState;
using BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState;
using BlockBase.Runtime.Requester.StateMachine.SidechainProductionState;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlockBase.Runtime.Requester
{
    public class SidechainMaintainerManager2 : ISidechainMaintainerManager2
    {
        private SidechainMaintainerStateManager _sidechainMaintainerStateManager;
        private SidechainProductionStateManager _sidechainProductionStateManager;
        private PeerConnectionStateManager _peerConnectionStateManager;

        private IMongoDbProducerService _mongoDbProducerService;

        private NodeConfigurations _nodeConfigurations;

        private SidechainPool _sidechainPool;

        private TransactionsHandler _transactionsHandler;


        public TaskContainer TaskContainerMaintainer { get; private set; }
        public TaskContainer TaskContainerProduction { get; private set; }
        public TaskContainer TaskContainerConnections { get; private set; }


        public SidechainMaintainerManager2(ILogger<ISidechainMaintainerManager2> logger, IMainchainService mainchainService, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, TransactionsHandler transactionsHandler, IMongoDbProducerService mongoDbProducerService, PeerConnectionsHandler peerConnectionsHandler)
        {

            _transactionsHandler = transactionsHandler;
            _sidechainPool = new SidechainPool(nodeConfigurations.Value.AccountName);
            _sidechainMaintainerStateManager = new SidechainMaintainerStateManager(logger, mainchainService, nodeConfigurations.Value);
            _sidechainProductionStateManager = new SidechainProductionStateManager(logger, mainchainService, nodeConfigurations.Value, transactionsHandler, mongoDbProducerService);
            _peerConnectionStateManager = new PeerConnectionStateManager(_sidechainPool, peerConnectionsHandler, nodeConfigurations.Value, networkConfigurations.Value, logger, mainchainService);

            _mongoDbProducerService = mongoDbProducerService;
            _nodeConfigurations = nodeConfigurations.Value;
        }

        public bool IsMaintainerRunning()
        {
            return TaskContainerMaintainer != null && TaskContainerMaintainer.Task.Status == TaskStatus.Running;
        }

        public bool IsProductionRunning()
        {
            return TaskContainerProduction != null && TaskContainerProduction.Task.Status == TaskStatus.Running;
        }

        public bool IsConnectionsManagerRunning()
        {
            return TaskContainerConnections != null && TaskContainerConnections.Task.Status == TaskStatus.Running;
        }

        public async Task Start()
        {
            await _transactionsHandler.Setup();
            
            if (!IsMaintainerRunning()) TaskContainerMaintainer = _sidechainMaintainerStateManager.Start();
            if (!IsProductionRunning()) TaskContainerProduction = _sidechainProductionStateManager.Start();
            if (!IsConnectionsManagerRunning()) TaskContainerConnections = _peerConnectionStateManager.Start();
        }

        public Task Pause()
        {
            //TODO rpinto - what is the best way to do this pause
            return Task.CompletedTask;
        }

        public async Task End()
        {

            if (IsMaintainerRunning()) TaskContainerMaintainer.CancellationTokenSource.Cancel();
            if(IsProductionRunning()) TaskContainerProduction.CancellationTokenSource.Cancel();
            if(IsConnectionsManagerRunning()) TaskContainerConnections.CancellationTokenSource.Cancel();
            await _mongoDbProducerService.DropRequesterDatabase(_nodeConfigurations.AccountName);

        }
    }
}