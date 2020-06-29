using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Provider.StateMachine.PeerConectionState;
using BlockBase.Runtime.Provider.StateMachine.SidechainState.States;
using System.Threading;

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

        private TransactionValidationsHandler _transactionValidationsHandler;


        private bool _inAutomaticMode = false;



        //TODO rpinto - it will be the state manager that besides coordinating state changes also is responsible to start the connectionchecker
        public SidechainStateManager(
            SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations,
            ILogger logger, INetworkService networkService,
            IMongoDbProducerService mongoDbProducerService, IMainchainService mainchainService,
             BlockRequestsHandler blockSender,
            TransactionValidationsHandler transactionValidationsHandler,
            bool automatic = false) : base(logger)
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
            _inAutomaticMode = automatic;
        }

        public override void Stop()
        {
            if(TaskContainer != null) TaskContainer.Stop();
            if(_blockProductionTaskContainer != null) _blockProductionTaskContainer.Stop();
            if(_peerConnectionTaskContainer != null) _peerConnectionTaskContainer.Stop();
        }

        protected override async Task Run()
        {
            var currentState = BuildState(typeof(StartState).Name);

            while (true)
            {
                var nextStateName = await currentState.Run(TaskContainer.CancellationTokenSource.Token);
                currentState = BuildState(nextStateName);

                if (currentState.GetType() == typeof(EndState))
                {
                    await currentState.Run(default(CancellationToken));
                    _blockProductionTaskContainer.Stop();
                    _peerConnectionTaskContainer.Stop();
                    if (_inAutomaticMode) await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(_sidechain.ClientAccountName);
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
                        _mongoDbProducerService, _blockSender, _transactionValidationsHandler);
                    _blockProductionTaskContainer = blockProductionStateManager.Start();

                    _logger.LogInformation($"Started block production");
                }
            }
        }

        protected override IState BuildState(string state)
        {
            if (state == typeof(StartState).Name) return new StartState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(CandidatureState).Name) return new CandidatureState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(SecretTimeState).Name) return new SecretTimeState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(IPSendTimeState).Name) return new IPSendTimeState(_sidechain, _logger, _mainchainService, _nodeConfigurations, _networkConfigurations);
            if (state == typeof(IPReceiveState).Name) return new IPReceiveState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(ProductionState).Name) return new ProductionState(_sidechain, _logger, _mainchainService, _nodeConfigurations);
            if (state == typeof(EndState).Name) return new EndState(_sidechain, _logger, _mongoDbProducerService);

            return null;
        }
    }
}
