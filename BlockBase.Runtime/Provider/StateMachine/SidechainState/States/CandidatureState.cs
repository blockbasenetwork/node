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

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class CandidatureState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private ContractInformationTable _contractInfo;
        private List<CandidateTable> _candidates;

        private SidechainPool _sidechainPool;
        
        public CandidatureState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_candidates.Any(c => c.Key == _nodeConfigurations.AccountName));
        }

        protected override async Task DoWork()
        {
            var secretHash = HashHelper.Sha256Data(HashHelper.Sha256Data(Encoding.ASCII.GetBytes(_nodeConfigurations.SecretPassword)));
            var addCandidateTransaction = await _mainchainService.AddCandidature(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, _nodeConfigurations.ActivePublicKey, HashHelper.ByteArrayToFormattedHexaString(secretHash), (int)_sidechainPool.ProducerType);
            
            _logger.LogDebug($"Sent candidature to chain {_sidechainPool.ClientAccountName} Tx: {addCandidateTransaction}");
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null) return Task.FromResult(false);
            return Task.FromResult(_contractStateTable.CandidatureTime || _contractStateTable.SecretTime);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isCandidateInTable = _candidates.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isCandidateInTable && _contractStateTable.SecretTime, typeof(SecretTimeState).Name));
        }

        protected override async Task UpdateStatus()
        {
            var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var candidates = await _mainchainService.RetrieveCandidates(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if(contractInfo == null) return;
            if(contractState == null) return;
            if(candidates == null) return;

            _contractInfo = contractInfo;
            _contractStateTable = contractState;
            _candidates = candidates;

            var timeDiff = _contractInfo.CandidatureEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            _delay = timeDiff > 0 ? TimeSpan.FromSeconds(timeDiff) : TimeSpan.FromSeconds(2);
        }
    }

}