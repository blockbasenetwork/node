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

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class SecretTimeState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private List<CandidateTable> _candidates;

        private SidechainPool _sidechainPool;
        public SecretTimeState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
        }

        protected override Task<bool> IsWorkDone()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var addedSecretToCandidate = _candidates.Where(c => c.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.Secret;

            return Task.FromResult(isProducerInTable || !string.IsNullOrWhiteSpace(addedSecretToCandidate));
        }

        protected override async Task DoWork()
        {
            var secret = HashHelper.Sha256Data(Encoding.ASCII.GetBytes(_nodeConfigurations.SecretPassword));
            var addSecretTransaction = await _mainchainService.AddSecret(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, HashHelper.ByteArrayToFormattedHexaString(secret));
        }

        protected override Task<bool> HasConditionsToContinue()
        {
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
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var candidates = await _mainchainService.RetrieveCandidates(_sidechainPool.ClientAccountName);
            var producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);

            _contractStateTable = contractState;
            _producers = producers;
            _candidates = candidates;
        }
    }

}