using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.Extensions.Logging;
using BlockBase.Domain.Configurations;
using BlockBase.Utils.Crypto;
using System.Text;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using System;
using System.Reflection;
using BlockBase.Utils;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class CandidatureState : ProviderAbstractState<StartState, EndState>
    {
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private ContractInformationTable _contractInfo;
        private List<CandidateTable> _candidates;

        private bool _inAutomaticMode;
        private bool _hasToAddStake;

        public CandidatureState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, bool inAutomaticMode) : base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
            _inAutomaticMode = inAutomaticMode;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_candidates.Any(c => c.Key == _nodeConfigurations.AccountName));
        }

        protected override async Task DoWork()
        {
            if (_hasToAddStake)
            {
                var stake = Math.Round((decimal)_contractInfo.Stake / 10000, 4);
                var stakeTransaction = await _mainchainService.AddStake(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, stake.ToString("F4") + " BBT");
            
                _logger.LogInformation($"Sent stake to chain {_sidechainPool.ClientAccountName} Tx: {stakeTransaction}");
            }
            else
            {
                var secretHash = HashHelper.Sha256Data(HashHelper.Sha256Data(Encoding.ASCII.GetBytes(_nodeConfigurations.SecretPassword)));
                var softwareVersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
                var softwareVersion = VersionHelper.ConvertFromVersionString(softwareVersionString);
                var addCandidateTransaction = await _mainchainService.AddCandidature(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, _nodeConfigurations.ActivePublicKey, HashHelper.ByteArrayToFormattedHexaString(secretHash), (int)_sidechainPool.ProducerType, softwareVersion);

                _logger.LogInformation($"Sent candidature to chain {_sidechainPool.ClientAccountName} Tx: {addCandidateTransaction}");
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if (_contractInfo == null || _contractStateTable == null) return Task.FromResult(false);
            return Task.FromResult(_contractStateTable.CandidatureTime || _contractStateTable.SecretTime);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isCandidateInTable = _candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isCandidateInTable && _contractStateTable.SecretTime, typeof(SecretTimeState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _candidates = await _mainchainService.RetrieveCandidates(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if (_contractInfo == null) return;

            if (_inAutomaticMode)
            {
                var stake = await _mainchainService.GetAccountStake(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
                if (stake == null) _hasToAddStake = true;

                var minimumProviderState = Math.Round((decimal)_contractInfo.Stake / 10000, 4);
                if (stake.Stake < minimumProviderState) _hasToAddStake = true;
            }

            var timeDiff = _contractInfo.CandidatureEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _delay = timeDiff > 0 ? TimeSpan.FromSeconds(timeDiff) : TimeSpan.FromSeconds(2);
        }
    }

}