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

        private NetworkConfigurations _networkConfigurations;
        private INetworkService _networkService;
        private bool _isNodeSynchronized;
        private bool _isReadyToProduce;


        public SynchronizeNodeState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations,
             INetworkService networkService) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;

            _networkService = networkService;
            _isNodeSynchronized = false;
            _isReadyToProduce = false;
        }

        protected override async Task DoWork()
        {
            OpResult<bool> opResult = null;
            if (!_isNodeSynchronized)
            {
                //synchronizes the node - it may abort synchronization if it fails to receive blocks for too long
                var syncResult = await _mongoDbProducerService.TrySynchronizeDatabaseWithSmartContract(_sidechainPool.ClientAccountName, _lastSubmittedBlockHeader.BlockHash, _currentProducer.StartProductionTime);

                if(!syncResult)
                {
                    _logger.LogDebug("Producer not up to date, building chain.");
                    opResult = await SyncChain();
                    _isNodeSynchronized = opResult.Succeeded;
                }
                else
                {
                    _isNodeSynchronized = true;
                }
                
            }

            if (_isNodeSynchronized && !_isReadyToProduce)
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

            var lastSubmittedBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            _isReadyToProduce = producerList.Any(p => p.Key == _nodeConfigurations.AccountName && p.IsReadyToProduce);
            _contractStateTable = contractState;
            _producerList = producerList;
            _currentProducer = currentProducer;
            _lastSubmittedBlockHeader = lastSubmittedBlockHeader;

            if(lastSubmittedBlockHeader == null)
                _isNodeSynchronized = true;

        }

        private async Task<OpResult<bool>> SyncChain()
        {
            _logger.LogDebug("Building chain.");
            var chainBuilder = new ChainBuilder2(_logger, _sidechainPool, _mongoDbProducerService, _nodeConfigurations, _networkService, _mainchainService, _networkConfigurations.GetEndPoint());
            var opResult = await chainBuilder.Run();

            return opResult;
        }


    }
}