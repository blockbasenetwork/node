using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.Extensions.Logging;
using BlockBase.Utils.Crypto;
using System.Text;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class SecretTimeState : AbstractState
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private List<CandidateTable> _candidates;
        public SecretTimeState(CurrentGlobalStatus status, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(status, logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override async Task<bool> IsWorkDone()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var addedSecretToCandidate = _candidates.Where(c => c.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.Secret;

            return isProducerInTable || !string.IsNullOrWhiteSpace(addedSecretToCandidate);
        }

        protected override async Task DoWork()
        {
            var secret = HashHelper.Sha256Data(Encoding.ASCII.GetBytes(_nodeConfigurations.SecretPassword));
            var addSecretTransaction = await _mainchainService.AddSecret(Status.Local.ClientAccountName, _nodeConfigurations.AccountName, HashHelper.ByteArrayToFormattedHexaString(secret));
        }

        protected override async Task<bool> HasConditionsToContinue()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var isCandidateInTable = _candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

            return (_contractStateTable.SecretTime || _contractStateTable.IPSendTime) && (isProducerInTable || isCandidateInTable);
        }

        protected override async Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return (isProducerInTable && _contractStateTable.IPSendTime, typeof(IPSendTimeState).Name);
        }

        protected override async Task UpdateStatus()
        {
            var contractState = await _mainchainService.RetrieveContractState(Status.Local.ClientAccountName);
            var candidates = await _mainchainService.RetrieveCandidates(Status.Local.ClientAccountName);
            var producers = await _mainchainService.RetrieveProducersFromTable(Status.Local.ClientAccountName);

            _contractStateTable = contractState;
            _producers = producers;
            _candidates = candidates;
        }
    }

}