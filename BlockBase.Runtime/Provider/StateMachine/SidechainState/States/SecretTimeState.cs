using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.Extensions.Logging;
using BlockBase.Utils.Crypto;
using System.Text;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using System;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class SecretTimeState : ProviderAbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private ContractInformationTable _contractInfo;
        private List<ProducerInTable> _producers;
        private List<CandidateTable> _candidates;
        private const string _emptySecretString = "0000000000000000000000000000000000000000000000000000000000000000";

        private SidechainPool _sidechainPool;
        public SecretTimeState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger, sidechainPool)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
        }

        protected override Task<bool> IsWorkDone()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var addedSecretToCandidate = _candidates.Where(c => c.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.Secret;

            return Task.FromResult(isProducerInTable || !addedSecretToCandidate.Equals(_emptySecretString));
        }

        protected override async Task DoWork()
        {
            var secret = HashHelper.Sha256Data(Encoding.ASCII.GetBytes(_nodeConfigurations.SecretPassword));
            var addSecretTransaction = await _mainchainService.AddSecret(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, HashHelper.ByteArrayToFormattedHexaString(secret));
        
            _logger.LogDebug($"Sent secret {secret} Tx: {addSecretTransaction}");
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _contractInfo == null || _candidates == null || _producers == null) return Task.FromResult(false);
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var isCandidateInTable = _candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((_contractStateTable.SecretTime || _contractStateTable.IPSendTime) && (isProducerInTable || isCandidateInTable));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isProducerInTable && _contractStateTable.IPSendTime, typeof(IPSendTimeState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _candidates = await _mainchainService.RetrieveCandidates(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if(_contractInfo == null) return;
        
            var timeDiff = _contractInfo.SecretEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _delay = timeDiff > 0 ? TimeSpan.FromSeconds(timeDiff) : TimeSpan.FromSeconds(2);
        }
    }

}