using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
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

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    public class SynchronizeValidatorNodeState : AbstractState<StartState, EndState>
    {

        private IMainchainService _mainchainService;
        private ContractStateTable _contractStateTable;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private BlockheaderTable _lastSubmittedBlockHeader;
        private SidechainPool _sidechainPool;
        private NetworkConfigurations _networkConfigurations;
        private INetworkService _networkService;
        private bool _isNodeSynchronized;
        private bool _isReadyToProduce;
        private TransactionValidationsHandler _transactionValidationsHandler;


        public SynchronizeValidatorNodeState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, INetworkService networkService,
            TransactionValidationsHandler transactionValidationsHandler) : base(logger)
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

        protected override Task DoWork()
        {
            //needs to sync all previous blocks until he finds one with a transaction
            //needs to delete all blocks except that one
            throw new NotImplementedException();
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

            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if (contractState == null) return;
            if (producerList == null) return;
            if(currentProducer == null) return;


            _lastSubmittedBlockHeader = await WaitForAndRetrieveTheLastValidBlockHeaderInSmartContract(
                //TODO rpinto - check if this timespan can be better estimated
                currentProducer.StartProductionTime, TimeSpan.FromSeconds(5));


            _isReadyToProduce = producerList?.Any(p => p.Key == _nodeConfigurations.AccountName && p.IsReadyToProduce) ?? false;
            _contractStateTable = contractState;
            _producerList = producerList;
            _currentProducer = currentProducer;
        }


        private async Task<BlockheaderTable> WaitForAndRetrieveTheLastValidBlockHeaderInSmartContract(long currentStartProductionTime, TimeSpan delayBetweenRequests)
        {
            while (true)
            {
                var lastSubmittedBlock = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
                if (lastSubmittedBlock != null && lastSubmittedBlock.IsVerified && lastSubmittedBlock.Timestamp > currentStartProductionTime) return lastSubmittedBlock;
                await Task.Delay(delayBetweenRequests);
            }
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