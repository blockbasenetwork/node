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
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.StateMachine.SidechainState;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using BlockBase.Utils.Operation;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    public class SynchronizeNodeState : AbstractState<StartState, EndState>
    {

        private IMainchainService _mainchainService;
        private ContractStateTable _contractStateTable;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private BlockheaderTable _lastSubmittedBlockHeader;
        private SidechainPool _sidechainPool;

        private ISidechainDatabasesManager _sidechainDatabaseManager;
        private NetworkConfigurations _networkConfigurations;
        private INetworkService _networkService;
        private bool _isNodeSynchronized;
        private bool _isReadyToProduce;


        public SynchronizeNodeState(ILogger logger, IMainchainService mainchainService, 
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool, 
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, 
            ISidechainDatabasesManager sidechainDatabaseManager, INetworkService networkService) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainDatabaseManager = sidechainDatabaseManager;
            _networkService = networkService;
            _isNodeSynchronized = false;
            _isReadyToProduce = false;
        }

        protected override async Task DoWork()
        {
            //if it's a validator the work is done
            if (_sidechainPool.ProducerType == ProducerTypeEnum.Validator)
            {
                await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
                return;
            }

            //synchronizes the node - it may abort synchronization if it fails to receive blocks for too long
            var syncResult = await _mongoDbProducerService.TrySynchronizeDatabaseWithSmartContract(_sidechainPool.ClientAccountName, _lastSubmittedBlockHeader.BlockHash, _currentProducer.StartProductionTime);

            _logger.LogDebug("Producer not up to date, building chain.");

            //TODO rpinto - does the provider have enough time to build the chain before being banned?
            var opResult = await SyncChain(TimeSpan.FromSeconds(5));

            if(opResult.Succeeded)
            {
                await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //verifies if he is a producer and the sidechain is in production state
            return Task.FromResult(_contractStateTable.ProductionTime && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_sidechainPool.ProducerType == ProducerTypeEnum.Validator && _isReadyToProduce) return Task.FromResult((true, typeof(NetworkReactionState).Name));

            //verifies if he is synchronized and ready to produce
            if (_isNodeSynchronized && _isReadyToProduce) return Task.FromResult((true, typeof(NetworkReactionState).Name));

            return Task.FromResult((false, typeof(NetworkReactionState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            //if it's a validator the work is done if he's ready to produce
            if (_sidechainPool.ProducerType == ProducerTypeEnum.Validator && _isReadyToProduce) return Task.FromResult(true);

            //verifies if he is synchronized and ready to produce
            return Task.FromResult(_isNodeSynchronized && _isReadyToProduce);
        }

        protected override async Task UpdateStatus()
        {
            //if it's a validator no need to update anything
            if (_sidechainPool.ProducerType == ProducerTypeEnum.Validator) return;

            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);

            var lastSubmittedBlockHeader = await WaitForAndRetrieveTheLastValidBlockHeaderInSmartContract(
                //TODO rpinto - check if this timespan can be better estimated
                currentProducer.StartProductionTime, TimeSpan.FromSeconds(5));

            _isReadyToProduce = producerList.Any(p => p.Key == _nodeConfigurations.AccountName && p.IsReadyToProduce);
            _contractStateTable = contractState;
            _producerList = producerList;
            _currentProducer = currentProducer;
            _lastSubmittedBlockHeader = lastSubmittedBlockHeader;
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

        private async Task<OpResult<bool>> SyncChain(TimeSpan delayBetweenProgressChecks)
        {
            _logger.LogDebug("Building chain.");
            var chainBuilder = new ChainBuilder2(_logger, _sidechainPool, _mongoDbProducerService, _sidechainDatabaseManager, _nodeConfigurations, _networkService, _mainchainService, _networkConfigurations.GetEndPoint());
            var opResult = await chainBuilder.Run();

            return opResult;
        }


    }
}