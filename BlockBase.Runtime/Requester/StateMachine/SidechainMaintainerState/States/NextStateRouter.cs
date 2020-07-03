using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class NextStateRouter : AbstractMainchainState<StartState, EndState>
    {
        private string _nextState;
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private IMainchainService _mainchainService;
        private bool _hasEnoughStake;

        private NodeConfigurations _nodeConfigurations;

        public NextStateRouter(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
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
            return Task.FromResult((_nextState != null, _nextState));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(false);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);

            if (_contractState == null || _contractInfo == null) return;

            _hasEnoughStake = await HasEnoughStakeUntilNextSettlement();

            _nextState = GetNextSidechainState(_contractInfo, _contractState);

            if (_nextState == null)
            {
                _logger.LogDebug($"{this.GetType().Name} - Nothing to do to maintain...delaying");
                _delay = TimeSpan.FromSeconds(10);
            }
            else
            {
                _delay = TimeSpan.FromSeconds(3);
            }
        }

        private string GetNextSidechainState(ContractInformationTable contractInfo, ContractStateTable contractState)
        {
            if (contractState.ConfigTime)
                return typeof(CandidatureReceivalState).Name;
            if (contractState.CandidatureTime && IsTimeUpForSidechainPhase(contractInfo.CandidatureEndDate, 0))
                return typeof(SecretSharingState).Name;
            if (contractState.SecretTime && IsTimeUpForSidechainPhase(contractInfo.SecretEndDate, 0))
                return typeof(IPSharingState).Name;
            if (contractState.IPSendTime && IsTimeUpForSidechainPhase(contractInfo.SendEndDate, 0))
                return typeof(ProvidersConnectionState).Name;
            if (contractState.IPReceiveTime && IsTimeUpForSidechainPhase(contractInfo.ReceiveEndDate, 0))
                return typeof(StartProductionState).Name;
            return null;
        }

        private async Task<bool> HasEnoughStakeUntilNextSettlement()
        {
            decimal requesterStake = 0;
            var accountStake = await _mainchainService.GetAccountStake(_nodeConfigurations.AccountName, _nodeConfigurations.AccountName);
            if (accountStake == null) return false;

            var stakeString = accountStake.Stake?.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            decimal.TryParse(stakeString, out requesterStake);

            var maxPaymentPerBlock = new[] { _contractInfo.MaxPaymentPerBlockFullProducers, _contractInfo.MaxPaymentPerBlockHistoryProducers, _contractInfo.MaxPaymentPerBlockValidatorProducers }.Max();
            var neededBBT = _contractInfo.BlocksBetweenSettlement * _contractInfo.MaxPaymentPerBlockFullProducers;
            var neededBBTDecimal = Math.Round((decimal)neededBBT / 10000, 4);

            return (requesterStake >= neededBBTDecimal);
        }

        private TimeSpan CalculateNextDelay()
        {
            return TimeSpan.FromMinutes(1);
            //TODO
        }
    }
}