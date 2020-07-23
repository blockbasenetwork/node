using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionState.States
{
    public class StartState : AbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private readonly IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        public StartState( ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations): base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
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
            return Task.FromResult(_contractStateTable != null);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_contractStateTable != null, typeof(NextStateRouter).Name));
        }

        protected override async Task UpdateStatus() 
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
        }
    }
}