using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antlr4.Runtime;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryParser;
using BlockBase.Domain;
using BlockBase.Domain.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Operation;
using Microsoft.Extensions.Logging;
using BlockBase.Runtime.Sql;

namespace BlockBase.Runtime.Provider.StateMachine.BlockProductionState.States
{
    public class SynchronizeNodeState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {

        private ContractStateTable _contractStateTable;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private BlockheaderTable _lastValidSubmittedBlockHeader;
        private BlockheaderTable _lastIrreversibleValidBlockHeader;

        private NetworkConfigurations _networkConfigurations;
        private INetworkService _networkService;
        private bool _isNodeSynchronized;
        private bool _isReadyToProduce;
        private IConnector _connector;
        TransactionValidationsHandler _transactionValidationsHandler;
        PeerConnectionsHandler _peerConnectionsHandler;

        private SqlExecutionHelper _sqlExecutionHelper;

        public SynchronizeNodeState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations,
            INetworkService networkService, TransactionValidationsHandler transactionValidationsHandler,
            IConnector connector, PeerConnectionsHandler peerConnectionsHandler) : base(logger, sidechainPool, mainchainService)
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

            _transactionValidationsHandler = transactionValidationsHandler;
            _connector = connector;

            _peerConnectionsHandler = peerConnectionsHandler;
            _sqlExecutionHelper = new SqlExecutionHelper(connector);
        }

        protected override async Task DoWork()
        {
            OpResult<bool> opResult = null;

            if (!_isNodeSynchronized)
            {
                if (!await _mongoDbProducerService.IsBlockConfirmed(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash))
                {
                    await _mongoDbProducerService.ConfirmBlock(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash);
                }

                //synchronizes the node - it may abort synchronization if it fails to receive blocks for too long
                var syncResult = await _mongoDbProducerService.TrySynchronizeDatabaseWithSmartContract(_sidechainPool.ClientAccountName, _lastValidSubmittedBlockHeader.BlockHash, _currentProducer.StartProductionTime, _sidechainPool.ProducerType);

                if (_sidechainPool.ProducerType == ProducerTypeEnum.Full)
                    await ExecutePendingTransactions();


                if (!syncResult)
                {
                    _logger.LogInformation("Producer not up to date, building chain.");
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
            //check preconditions to continue update
            if (_contractStateTable == null) return;
            if (_producerList == null) return;

            _lastValidSubmittedBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
            
            if (_sidechainPool.ProducerType == ProducerTypeEnum.Full)
                _lastIrreversibleValidBlockHeader = await _mainchainService.GetLastIrreversibleBlockHeader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            if (_lastValidSubmittedBlockHeader == null)
                _isNodeSynchronized = true;

            _isReadyToProduce = _producerList.Any(p => p.Key == _nodeConfigurations.AccountName && p.IsReadyToProduce);
        }

        private async Task ExecutePendingTransactions()
        {
            if (_lastIrreversibleValidBlockHeader == null || _lastIrreversibleValidBlockHeader.LastTransactionSequenceNumber == 0) return;

            var transactionToExecute = await _mongoDbProducerService.GetTransactionToExecute(_sidechainPool.ClientAccountName, Convert.ToInt64(_lastIrreversibleValidBlockHeader.LastTransactionSequenceNumber));

            if (transactionToExecute != null && !await _sqlExecutionHelper.HasTransactionBeenExecuted(transactionToExecute))
                await ExecuteTransaction(transactionToExecute.TransactionFromTransactionDB());

            var currentSequenceNumber = transactionToExecute?.SequenceNumber ?? 0;

            var subsequentTransactions = (await _mongoDbProducerService.GetTransactionsSinceSequenceNumber(_sidechainPool.ClientAccountName, Convert.ToUInt64(currentSequenceNumber))).OrderBy(t => t.SequenceNumber);

            foreach (var transaction in subsequentTransactions)
            {
                if(transaction.SequenceNumber != Convert.ToUInt64(currentSequenceNumber) + 1) return;
                await ExecuteTransaction(transaction);
                currentSequenceNumber++;
            }
            return;
        }

       

        private async Task ExecuteTransaction(Transaction transaction)
        {
            var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
            await _mongoDbProducerService.UpdateTransactionToExecute(_sidechainPool.ClientAccountName, transactionDB);

            if (transactionDB.DatabaseName != "")
                await _connector.ExecuteCommandWithTransactionNumber(transactionDB.TransactionJson, transactionDB.DatabaseName, Convert.ToUInt64(transactionDB.SequenceNumber), _sidechainPool.ClientAccountName);
            else
                await _connector.ExecuteCommand(transactionDB.TransactionJson, transactionDB.DatabaseName, _sidechainPool.ClientAccountName);

        }

        private async Task<OpResult<bool>> SyncChain()
        {
            UpdateConnections();
            _logger.LogDebug("Building chain.");
            var chainBuilder = new ChainBuilder(_logger, _sidechainPool, _mongoDbProducerService, _nodeConfigurations, _networkService, _mainchainService, _networkConfigurations.GetEndPoint(), _transactionValidationsHandler);
            var opResult = await chainBuilder.Run();

            return opResult;
        }

        private void UpdateConnections()
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();

            var producersInPool = _producerList.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    NewlyJoined = false,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    IPEndPoint = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()?.IPEndPoint
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
        }
    }
}