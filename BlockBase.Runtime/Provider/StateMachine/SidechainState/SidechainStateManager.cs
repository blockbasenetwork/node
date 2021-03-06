using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Provider.StateMachine.PeerConnectionState;
using BlockBase.Runtime.Provider.StateMachine.SidechainState.States;
using System.Threading;
using BlockBase.Runtime.Provider.StateMachine.SidechainState.HistoryValidationState;
using BlockBase.DataPersistence.Sidechain.Connectors;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState
{
    public class SidechainStateManager : AbstractStateManager<StartState, EndState>
    {
        private ILogger _logger;
        private IMainchainService _mainchainService;
        private INetworkService _networkService;
        private IMongoDbProducerService _mongoDbProducerService;

        private SidechainPool _sidechain;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private BlockRequestsHandler _blockSender;

        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private TaskContainer _blockProductionTaskContainer;
        private TaskContainer _peerConnectionTaskContainer;
        private TaskContainer _historyValidationTaskContainer;

        private TransactionValidationsHandler _transactionValidationsHandler;
        private ISidechainProducerService _sidechainProducerService;
        private bool _inAutomaticMode = false;
        private IConnector _connector;



        //TODO rpinto - it will be the state manager that besides coordinating state changes also is responsible to start the connectionchecker
        public SidechainStateManager(
            SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations,
            ILogger logger, INetworkService networkService,
            IMongoDbProducerService mongoDbProducerService, IMainchainService mainchainService,
             BlockRequestsHandler blockSender,
            TransactionValidationsHandler transactionValidationsHandler,
            ISidechainProducerService sidechainProducerService,
            bool automatic, IConnector connector) : base(logger)
        {
            _sidechain = sidechain;
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _networkService = networkService;
            _mongoDbProducerService = mongoDbProducerService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _blockSender = blockSender;
            _transactionValidationsHandler = transactionValidationsHandler;
            _sidechainProducerService = sidechainProducerService;
            _inAutomaticMode = automatic;
            _connector = connector;
        }

        public override void Stop()
        {
            if(TaskContainer != null) TaskContainer.Stop();
            if(_blockProductionTaskContainer != null) _blockProductionTaskContainer.Stop();
            if(_peerConnectionTaskContainer != null) _peerConnectionTaskContainer.Stop();
            if(_historyValidationTaskContainer != null) _historyValidationTaskContainer.Stop();
        }

        protected override async Task Run()
        {
            var currentState = BuildState(typeof(StartState).Name);
            await _mongoDbProducerService.CreateCollections(_sidechain.ClientAccountName);

            while (true)
            {
                var nextStateName = await currentState.Run(TaskContainer.CancellationTokenSource.Token);
                currentState = BuildState(nextStateName);

                if (currentState.GetType() == typeof(EndState))
                {
                    await currentState.Run(default(CancellationToken));
                    _blockProductionTaskContainer.Stop();
                    _peerConnectionTaskContainer.Stop();
                    this.Stop();
                    return;
                }


                //TODO - rpinto this means we're going to have a peerConnectionStateManager per sidechain, right?
                if (_peerConnectionTaskContainer == null && (currentState.GetType() == typeof(IPReceiveState) || currentState.GetType() == typeof(ProductionState)))
                {
                    var peerConnectionStateManager = new PeerConnectionStateManager(_sidechain, _peerConnectionsHandler, _nodeConfigurations, _networkConfigurations, _logger, _mainchainService);
                    _peerConnectionTaskContainer = peerConnectionStateManager.Start();

                    _logger.LogInformation("Started peer connection state manager");
                }

                if (currentState.GetType() == typeof(ProductionState) && _blockProductionTaskContainer == null)
                {
                    var blockProductionStateManager = new BlockProductionStateManager(
                        _logger, _sidechain, _nodeConfigurations, _networkConfigurations,
                        _networkService, _peerConnectionsHandler, _mainchainService,
                        _mongoDbProducerService, _blockSender, _transactionValidationsHandler, _connector);
                    _blockProductionTaskContainer = blockProductionStateManager.Start();

                    var historyValidationStateManager = new HistoryValidationStateManager(_logger, _sidechain, _nodeConfigurations, _mainchainService, _mongoDbProducerService);
                    _historyValidationTaskContainer  = historyValidationStateManager.Start();
                    _logger.LogInformation($"Started block production and history validation.");
                }
            }
        }

        protected override IState BuildState(string state)
        {
            if (state == typeof(StartState).Name) return new StartState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(CandidatureState).Name) return new CandidatureState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _inAutomaticMode);
            if (state == typeof(SecretTimeState).Name) return new SecretTimeState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(IPSendTimeState).Name) return new IPSendTimeState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations);
            if (state == typeof(IPReceiveState).Name) return new IPReceiveState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _peerConnectionsHandler, _mongoDbProducerService);
            if (state == typeof(ProductionState).Name) return new ProductionState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations, _mongoDbProducerService);
            if (state == typeof(UpdateIpState).Name) return new UpdateIpState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations);
            if (state == typeof(UpdateKeyState).Name) return new UpdateKeyState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations);
            if (state == typeof(EndState).Name) return new EndState(_sidechain, _logger, _mongoDbProducerService, _sidechainProducerService, _mainchainService, _inAutomaticMode);
            if (state == typeof(WaitForEndConfirmationState).Name) return new WaitForEndConfirmationState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            
            return null;
        }
    }
}
