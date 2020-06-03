using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.Extensions.Logging;
using BlockBase.Domain.Configurations;
using BlockBase.Utils.Crypto;
using System.Text;

namespace BlockBase.Runtime.SidechainState.States
{
    public class CandidatureState : AbstractState
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<CandidateTable> _candidates;
        
        public CandidatureState(CurrentGlobalStatus status, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(status, logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_candidates.Any(c => c.Key == _nodeConfigurations.AccountName));
        }

        protected override async Task DoWork()
        {
            var secretHash = HashHelper.Sha256Data(HashHelper.Sha256Data(Encoding.ASCII.GetBytes(_nodeConfigurations.SecretPassword)));
            var addCandidateTransaction = await _mainchainService.AddCandidature(Status.Local.ClientAccountName, _nodeConfigurations.AccountName, _nodeConfigurations.ActivePublicKey, HashHelper.ByteArrayToFormattedHexaString(secretHash), (int)Status.Local.ProducerType);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractStateTable.CandidatureTime || _contractStateTable.SecretTime);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isCandidateInTable = _candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isCandidateInTable && _contractStateTable.SecretTime, typeof(SecretTimeState).Name));
        }

        protected override async Task UpdateStatus()
        {
            var contractState = await _mainchainService.RetrieveContractState(Status.Local.ClientAccountName);
            var candidates = await _mainchainService.RetrieveCandidates(Status.Local.ClientAccountName);
            
            _contractStateTable = contractState;
            _candidates = candidates;
        }
    }

}