using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class IPReceiveState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private ContractInformationTable _contractInfo;
        private List<ProducerInTable> _producers;
        private IMongoDbProducerService _mongoDbProducerService;
        private bool _isUpToDate;

        public IPReceiveState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler, IMongoDbProducerService mongoDbProducerService) : base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
            _peerConnectionsHandler = peerConnectionsHandler;
            _mongoDbProducerService = mongoDbProducerService;
        }

        protected override Task<bool> IsWorkDone()
        {
            var hasWorkToDo = _isUpToDate && !_producers.SingleOrDefault(p => p.Key == _nodeConfigurations.AccountName).IsReadyToProduce ? true : false;

            return Task.FromResult(!hasWorkToDo);
        }

        protected override async Task DoWork()
        {
            _peerConnectionsHandler.AddKnownSidechain(_sidechainPool);
            var isReadyToProduceTransaction = await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
            
            _logger.LogInformation($"Sent ready to produce transaction. Tx: {isReadyToProduceTransaction}");
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _contractInfo == null || _producers == null) return Task.FromResult(false);

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
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            
            var latestBlockHeader = await _mainchainService.RetrieveLastBlockFromLastSettlement(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
            
            _isUpToDate = latestBlockHeader != null ? await _mongoDbProducerService.IsBlockInDatabase(_sidechainPool.ClientAccountName, latestBlockHeader.BlockHash) : true;

            //check preconditions to continue update
            if(_contractInfo == null) return;

            var timeDiff = _contractInfo.ReceiveEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _delay = timeDiff > 0 ? TimeSpan.FromSeconds(timeDiff) : TimeSpan.FromSeconds(2);
        }
    }

}