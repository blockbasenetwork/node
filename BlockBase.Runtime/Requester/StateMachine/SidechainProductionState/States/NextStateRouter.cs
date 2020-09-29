using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainProductionState.States
{
    public class NextStateRouter : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private string _nextState;
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private CurrentProducerTable _currentProducer;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private BlockheaderTable _lastBlock;
        private bool _hasEnoughStake;
        private bool _currentProducerHasEnoughSignatures;

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
            return Task.FromResult(_contractState != null && _contractInfo != null && _currentProducer != null);
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
            _currentProducer = await _mainchainService.RetrieveCurrentProducer(_nodeConfigurations.AccountName);
            _hasEnoughStake = await HasEnoughStakeUntilNextSettlement();
            _lastBlock = await _mainchainService.GetLastSubmittedBlockheader(_nodeConfigurations.AccountName, Convert.ToInt32(_contractInfo.BlocksBetweenSettlement));

            var verifySignatures = await _mainchainService.RetrieveVerifySignatures(_nodeConfigurations.AccountName);
            var blockHash = verifySignatures.Where(v => v.Account == _currentProducer.Producer).SingleOrDefault()?.BlockHash;
            var producers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
            if (blockHash != null)
            {
                _currentProducerHasEnoughSignatures = CheckIfBlockHasMajorityOfSignatures(verifySignatures, blockHash, producers.Count, producers.Select(p => p.PublicKey).ToList());
            }
            
            if(_contractState == null || _contractInfo == null || _currentProducer == null) return;

            _nextState = GetNextSidechainState(_contractInfo, _contractState, _currentProducer);

            if (_nextState == null)
            {
                _logger.LogDebug($"{this.GetType().Name} - Nothing to do to maintain...delaying");
                _delay = TimeSpan.FromSeconds(15);
            }
            else
            {
                _delay = TimeSpan.FromSeconds(3);
            }
        }

        private string GetNextSidechainState(ContractInformationTable contractInfo, ContractStateTable contractState, CurrentProducerTable currentProducer)
        {
            if (_hasEnoughStake && IsTimeToSwitchProducer(contractInfo, contractState, currentProducer))
            {
                return typeof(SwitchProducerTurn).Name;
            }

            if (_currentProducerHasEnoughSignatures && _lastBlock != null && !_lastBlock.IsVerified)
            {
                return typeof(VerifyBlockState).Name;
            }


            return null;
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

        private bool CheckIfBlockHasMajorityOfSignatures(List<VerifySignature> verifySignatureTable, string blockHash, int numberOfProducers, List<string> requiredKeys)
        {
            var verifySignatures = verifySignatureTable?.Where(t => t.BlockHash == blockHash);
            var threshold = (numberOfProducers / 2) + 1;
            var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

            return verifySignatures?.Count() >= threshold;
        }

        private bool IsTimeToSwitchProducer(ContractInformationTable contractInfo, ContractStateTable contractState, CurrentProducerTable currentProducer)
        {
            return contractState.ProductionTime && currentProducer != null && IsTimeUpForProducer(currentProducer, contractInfo);
        }

        private TimeSpan CalculateNextDelay()
        {
            return TimeSpan.FromMinutes(1);
            //TODO
        }
    }
}