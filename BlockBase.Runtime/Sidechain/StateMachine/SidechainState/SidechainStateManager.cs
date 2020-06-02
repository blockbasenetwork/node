using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
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
        private NodeConfigurations _nodeConfigurations;

        public TaskContainer TaskContainer { get; private set; }

        

        public SidechainStateManager(SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, string endpoint, ILogger logger, INetworkService networkService, IMongoDbProducerService mongoDbProducerService, BlockSender blockSender, IMainchainService mainchainService)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
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
            var status = new CurrentGlobalStatus();
            var currentState = BuildState(typeof(StartState).Name, status);


            while(true)
            {
                var nextStateName = await currentState.Run();
                currentState = BuildState(nextStateName, status);

                if(currentState.GetType() == typeof(EndState))
                {
                    await currentState.Run();
                    break;
                }
            }
        }

        private AbstractState BuildState(string state, CurrentGlobalStatus status)
        {
            if(state == typeof(StartState).Name) return new StartState(status, _logger);
            if(state == typeof(CandidatureState).Name) return new CandidatureState(status, _logger, _mainchainService, _nodeConfigurations);

            return null;
        }

        
    }
}
