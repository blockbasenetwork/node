using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils.Operation;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.BlockProductionState.States
{
    public class SynchronizeValidatorNodeState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {

        private ContractStateTable _contractStateTable;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private BlockheaderTable _lastValidSubmittedBlockHeader;
        private NetworkConfigurations _networkConfigurations;
        private INetworkService _networkService;
        private bool _isNodeSynchronized;
        private bool _isReadyToProduce;
        private TransactionValidationsHandler _transactionValidationsHandler;


        public SynchronizeValidatorNodeState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, INetworkService networkService,
            TransactionValidationsHandler transactionValidationsHandler) : base(logger, sidechainPool, mainchainService)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _transactionValidationsHandler = transactionValidationsHandler;


            _networkService = networkService;
            _isNodeSynchronized = false;
            _isReadyToProduce = false;
        }

        protected override async Task DoWork()
        {
            OpResult<bool> opResult = null;

            if (!_isNodeSynchronized)
            {
                var syncResult = await _mongoDbProducerService.IsBlockInDatabase(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash);

                if (!syncResult)
                {
                    _logger.LogDebug("Validator producer doesn't have the last valid block.");
                    opResult = await SyncChain();
                    _isNodeSynchronized = opResult.Succeeded;
                }
                else
                {
                    _isNodeSynchronized = true;
                }

                if (!await _mongoDbProducerService.IsBlockConfirmed(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash))
                {
                    await _mongoDbProducerService.ConfirmBlock(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash);
                    await _mongoDbProducerService.ClearValidatorNode(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash, _lastValidSubmittedBlockHeader.TransactionCount);
                }
            }

            if (_isNodeSynchronized && !_isReadyToProduce)
            {
                await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
            }


        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if (_contractStateTable == null || _producerList == null) return Task.FromResult(false);
            //verifies if he is a producer and the sidechain is in production state
            return Task.FromResult(_contractStateTable.ProductionTime && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {

            //verifies if he is synchronized and ready to produce
            if (_isNodeSynchronized && _isReadyToProduce) return Task.FromResult((true, typeof(NetworkReactionState).Name));

            return Task.FromResult((false, typeof(NetworkReactionState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            //verifies if he is synchronized and ready to produce
            return Task.FromResult(_isNodeSynchronized && _isReadyToProduce);
        }

        protected override async Task UpdateStatus()
        {

            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);
            
            //check preconditions to continue update
            if (_contractStateTable == null) return;
            if (_producerList == null) return;
            if (_currentProducer == null) return;

            _lastValidSubmittedBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            if (_lastValidSubmittedBlockHeader == null)
                _isNodeSynchronized = true;

            _isReadyToProduce = _producerList?.Any(p => p.Key == _nodeConfigurations.AccountName && p.IsReadyToProduce) ?? false;
        }

        private async Task<OpResult<bool>> SyncChain()
        {
            _logger.LogDebug("Building chain.");
            var chainBuilder = new ChainBuilder(_logger, _sidechainPool, _mongoDbProducerService, _nodeConfigurations, _networkService, _mainchainService, _networkConfigurations.GetEndPoint(), _transactionValidationsHandler);
            var opResult = await chainBuilder.Run();

            return opResult;
        }


    }
}