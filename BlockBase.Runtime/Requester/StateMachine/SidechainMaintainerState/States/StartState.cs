using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class StartState : AbstractMainchainState<StartState, EndState>
    {
        private NodeConfigurations _nodeConfigurations;
        private IMainchainService _mainchainService;
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private bool _hasEnoughStake;

        public StartState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_hasEnoughStake && _contractState != null);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_contractState != null, typeof(NextStateRouter).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);

            if (_contractState == null || _contractInfo == null) return;

            _hasEnoughStake = await HasEnoughStakeUntilNextSettlement();
        }

        private async Task<bool> HasEnoughStakeUntilNextSettlement()
        {
            var accountStake = await _mainchainService.GetAccountStake(_nodeConfigurations.AccountName, _nodeConfigurations.AccountName);
            if (accountStake == null) return false;

            var maxPaymentPerBlock = new[] { _contractInfo.MaxPaymentPerBlockFullProducers, _contractInfo.MaxPaymentPerBlockHistoryProducers, _contractInfo.MaxPaymentPerBlockValidatorProducers }.Max();
            var neededBBT = _contractInfo.BlocksBetweenSettlement * maxPaymentPerBlock;
            var neededBBTDecimal = Math.Round((decimal)neededBBT / 10000, 4);

            return (accountStake?.Stake >= neededBBTDecimal);
        }
    }
}