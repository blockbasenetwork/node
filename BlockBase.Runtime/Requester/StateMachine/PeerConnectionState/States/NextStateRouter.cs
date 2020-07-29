using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using Microsoft.Extensions.Logging;


namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionState.States
{
    public class NextStateRouter : AbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        public NextStateRouter( ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractStateTable != null && (_contractStateTable.ProductionTime || _contractStateTable.IPReceiveTime));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_contractStateTable.ProductionTime || _contractStateTable.IPReceiveTime, typeof(ConnectToPeersState).Name));
        }

        protected override async Task UpdateStatus() 
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
        }
    }
}