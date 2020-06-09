using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class IPReceiveState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private ContractInformationTable _contractInfo;
        private List<ProducerInTable> _producers;
        private List<BlockheaderTable> _blockHeaders;

        private SidechainPool _sidechainPool;
        public IPReceiveState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
        }

        protected override Task<bool> IsWorkDone()
        {
            var hasWorkToDo = !_blockHeaders.Any() && !_producers.SingleOrDefault(p => p.Key == _nodeConfigurations.AccountName).IsReadyToProduce ? true : false;

            return Task.FromResult(!hasWorkToDo);
        }

        protected override async Task DoWork()
        {
            var isReadyToProduceTransaction = await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
            
            _logger.LogDebug($"Sent ready to produce transaction. Tx: {isReadyToProduceTransaction}");
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _contractInfo == null || _blockHeaders == null || _producers == null) return Task.FromResult(false);

            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            return Task.FromResult((_contractStateTable.IPReceiveTime || _contractStateTable.ProductionTime) && isProducerInTable);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isProducerInTable && _contractStateTable.ProductionTime, typeof(ProductionState).Name));
        }

        protected override async Task UpdateStatus()
        {
            var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            var producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var blockHeaders = await _mainchainService.RetrieveBlockheaderList(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
            

            //check preconditions to continue update
            if(contractInfo == null) return;
            if(contractState == null) return;
            if(blockHeaders == null) return;
            if(producers == null) return;

            _contractInfo = contractInfo;
            _producers = producers;
            _contractStateTable = contractState;
            _blockHeaders = blockHeaders;

            var timeDiff = _contractInfo.ReceiveEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _delay = timeDiff > 0 ? TimeSpan.FromSeconds(timeDiff) : TimeSpan.FromSeconds(2);
        }
    }

}