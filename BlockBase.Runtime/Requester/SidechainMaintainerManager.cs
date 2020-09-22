using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
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
    public class SidechainMaintainerManager : ISidechainMaintainerManager
    {
        private SidechainMaintainerStateManager _sidechainMaintainerStateManager;
        private SidechainProductionStateManager _sidechainProductionStateManager;
        private PeerConnectionStateManager _peerConnectionStateManager;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private SidechainPool _sidechainPool;
        private TransactionsManager _transactionsManager;


        public TaskContainer TaskContainerMaintainer { get; private set; }
        public TaskContainer TaskContainerProduction { get; private set; }
        public TaskContainer TaskContainerConnections { get; private set; }

        public TaskContainer TaskContainerTransactions { get; private set; }


        public SidechainMaintainerManager(ILogger<ISidechainMaintainerManager> logger, IMainchainService mainchainService, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, TransactionsManager transactionsManager, IMongoDbRequesterService mongoDbRequesterService, PeerConnectionsHandler peerConnectionsHandler)
        {

            _transactionsManager = transactionsManager;
            _sidechainPool = new SidechainPool(nodeConfigurations.Value.AccountName);
            _sidechainMaintainerStateManager = new SidechainMaintainerStateManager(logger, mainchainService, nodeConfigurations.Value);
            _sidechainProductionStateManager = new SidechainProductionStateManager(logger, mainchainService, nodeConfigurations.Value, transactionsManager);
            _peerConnectionStateManager = new PeerConnectionStateManager(_sidechainPool, peerConnectionsHandler, nodeConfigurations.Value, networkConfigurations.Value, logger, mainchainService);

            _mongoDbRequesterService = mongoDbRequesterService;
            _nodeConfigurations = nodeConfigurations.Value;
        }

        public bool IsMaintainerRunning()
        {
            return TaskContainerMaintainer != null && TaskContainerMaintainer.IsRunning();
        }

        public bool IsProductionRunning()
        {
            return TaskContainerProduction != null && TaskContainerProduction.IsRunning();
        }

        public bool IsConnectionsManagerRunning()
        {
            return TaskContainerConnections != null && TaskContainerConnections.IsRunning();
        }

        public bool IsTransactionsManagerRunning()
        {
            return TaskContainerTransactions != null && TaskContainerTransactions.IsRunning();
        }

        public async Task Start()
        {
            var sidechainInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);
            if (sidechainInfo != null && sidechainInfo.Key == _nodeConfigurations.AccountName)
            {
                await _transactionsManager.Setup();

                if (!IsMaintainerRunning()) TaskContainerMaintainer = _sidechainMaintainerStateManager.Start();
                if (!IsProductionRunning()) TaskContainerProduction = _sidechainProductionStateManager.Start();
                if (!IsConnectionsManagerRunning()) TaskContainerConnections = _peerConnectionStateManager.Start();
                if (!IsTransactionsManagerRunning()) TaskContainerTransactions = _transactionsManager.Start();
            }
        }

        public Task Pause()
        {
            //TODO rpinto - what is the best way to do this pause
            return Task.CompletedTask;
        }

        public async Task End()
        {

            if (IsMaintainerRunning()) TaskContainerMaintainer.CancellationTokenSource.Cancel();
            if (IsProductionRunning()) TaskContainerProduction.CancellationTokenSource.Cancel();
            if (IsConnectionsManagerRunning()) TaskContainerConnections.CancellationTokenSource.Cancel();
            if (IsTransactionsManagerRunning()) TaskContainerTransactions.CancellationTokenSource.Cancel();
            await _mongoDbRequesterService.DropRequesterDatabase(_nodeConfigurations.AccountName);

        }
    }
}