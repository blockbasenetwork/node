using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Provider.StateMachine.HistoryValidation.States
{
    public class StartState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producerList;
        private IList<MappedHistoryValidation> _historyValidations;
        public StartState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechain;
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
            if (_contractStateTable == null || _producerList == null || _historyValidations == null) return Task.FromResult(false);
            //TODO verifies if he is a producer
            return Task.FromResult(_producerList.Any(p => p.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_historyValidations.Any(t => !t.SignedProducers.Contains(_nodeConfigurations.AccountName) || t.Account == _nodeConfigurations.AccountName))
                return Task.FromResult((true, typeof(ValidateHistoryState).Name));

            return Task.FromResult((false, typeof(ValidateHistoryState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _historyValidations = await _mainchainService.RetrieveHistoryValidation(_sidechainPool.ClientAccountName);
        }
    }
}