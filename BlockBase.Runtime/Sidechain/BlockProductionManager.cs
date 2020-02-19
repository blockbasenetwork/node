using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Domain;

namespace BlockBase.Runtime.Sidechain
{
    public class BlockProductionManager : IThreadableComponent
    {
        public TaskContainer TaskContainer { get; private set; }
        private string _currentProducingProducerAccountName;
        private SidechainPool _sidechainPool;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private ChainBuilder _chainBuilder;
        private NodeConfigurations _nodeConfigurations;
        private string _endPoint;
        private BlockSender _blockSender;
        private long _nextTimeToCheckSmartContract;
        private long _previousTimeToCheck;
        private ILogger _logger;
        private IMongoDbProducerService _mongoDbProducerService;

        //TODO: change this when client specifies database type (MYSQL, SQL, ...)
        private ISidechainDatabasesManager _sidechainDatabaseManager;


        public BlockProductionManager(SidechainPool sidechainPool, NodeConfigurations nodeConfigurations, ILogger logger, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, string endPoint, BlockSender blockSender, ISidechainDatabasesManager sidechainDatabaseManager)
        {
            _logger = logger;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _peerConnectionsHandler = peerConnectionsHandler;
            _logger.LogDebug("Creating block producer.");
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _endPoint = endPoint;
            _blockSender = blockSender;
            _sidechainDatabaseManager = sidechainDatabaseManager;
            _chainBuilder = new ChainBuilder(_logger, _sidechainPool, _mongoDbProducerService, _sidechainDatabaseManager, _nodeConfigurations, _networkService, _mainchainService, _endPoint);
        }

        //TODO: Probably a good idea to protect from having a task already running in instance and replace taskcontainer with a new one and have multiple threads running per instance
        public TaskContainer Start()
        {
            TaskContainer = TaskContainer.Create(async () => await Execute());
            TaskContainer.Start();
            return TaskContainer;
        }

        public async Task Execute()
        {
            var databaseName = _sidechainPool.ClientAccountName;
            try
            {
                while (true)
                {
                    var _timeDiff = (_nextTimeToCheckSmartContract * 1000) - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (_timeDiff <= 0)
                    {
                        if (_nextTimeToCheckSmartContract == _previousTimeToCheck) await Task.Delay(10);
                        try
                        {
                            var currentProducerTable = (await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName)).SingleOrDefault();

                            if (currentProducerTable != null)
                            {
                                _nextTimeToCheckSmartContract = currentProducerTable.StartProductionTime + _sidechainPool.BlockTimeDuration;

                                if (_nextTimeToCheckSmartContract == _previousTimeToCheck) continue;
                                _logger.LogDebug($"StartProductionTime: {currentProducerTable.StartProductionTime}");
                                _logger.LogDebug($" Start Production Time: {DateTimeOffset.FromUnixTimeSeconds(currentProducerTable.StartProductionTime).UtcDateTime} Next time to check smart contract: {DateTimeOffset.FromUnixTimeSeconds(_nextTimeToCheckSmartContract).UtcDateTime}");

                                _previousTimeToCheck = _nextTimeToCheckSmartContract;
                                _currentProducingProducerAccountName = currentProducerTable.Producer;

                                await CancelProposalTransactionIfExists();

                                var lastValidBlockheaderSmartContractFromLastProduction = await _mainchainService.GetLastValidSubmittedBlockheaderFromLastProduction(_sidechainPool.ClientAccountName, currentProducerTable.StartProductionTime, (int)_sidechainPool.BlocksBetweenSettlement);
                                if (lastValidBlockheaderSmartContractFromLastProduction != null)
                                {
                                    if (!await _mongoDbProducerService.SynchronizeDatabaseWithSmartContract(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash, currentProducerTable.StartProductionTime))
                                    {
                                        await BuildChain();
                                    }

                                    if (!await _mongoDbProducerService.IsBlockConfirmed(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash))
                                    {
                                        var transactions = await _mongoDbProducerService.GetBlockTransactionsAsync(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash);
                                        //_sidechainDatabaseManager.ExecuteBlockTransactions(transactions);
                                        await _mongoDbProducerService.ConfirmBlock(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash);
                                    }
                                }

                                if (_currentProducingProducerAccountName == _nodeConfigurations.AccountName && !currentProducerTable.HasProducedBlock)
                                {
                                    var block = await ProduceBlock();
                                    await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(block, databaseName);
                                    await ProposeBlock(block);
                                }

                                await UpdatePeers();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical($"Error in block producer: {ex.Message}");
                            _logger.LogDebug($"Debug: {ex}");
                        }
                    }

                    else await Task.Delay((int)(_timeDiff));

                    TaskContainer.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Block producer was stopped.", ex);
            }
        }

        private async Task<Block> ProduceBlock()
        {
            var i = 0;
            while (i < 3)
            {
                try
                {
                    var lastBlock = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
                    uint currentSequenceNumber;
                    byte[] previousBlockhash;

                    if (lastBlock != null)
                    {
                        previousBlockhash = HashHelper.FormattedHexaStringToByteArray(lastBlock.BlockHash);
                        currentSequenceNumber = lastBlock.SequenceNumber + 1;
                    }
                    else
                    {
                        currentSequenceNumber = 1;
                        previousBlockhash = new byte[32];
                    }

                    var transactionsDatabaseName = _sidechainPool.ClientAccountName;
                    var allLooseTransactions = await _mongoDbProducerService.RetrieveLastLooseTransactions(transactionsDatabaseName);
                    ulong lastSequenceNumber = (await _mongoDbProducerService.LastIncludedTransaction(transactionsDatabaseName))?.SequenceNumber ?? 0;
                    var transactions = new List<Transaction>();

                    foreach (var looseTransaction in allLooseTransactions)
                    {
                        if (looseTransaction.SequenceNumber != lastSequenceNumber + 1) break;
                        lastSequenceNumber = looseTransaction.SequenceNumber;
                        transactions.Add(looseTransaction);
                        _logger.LogDebug($"Including transaction {lastSequenceNumber}");
                    }

                    var blockHeader = new BlockHeader()
                    {
                        Producer = _nodeConfigurations.AccountName,
                        BlockHash = new byte[0],
                        PreviousBlockHash = previousBlockhash,
                        SequenceNumber = currentSequenceNumber,
                        Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                        TransactionCount = (uint)transactions.Count(),
                        ProducerSignature = "",
                        MerkleRoot = MerkleTreeHelper.CalculateMerkleRootHash(transactions.Select(t => t.TransactionHash).ToList())
                    };

                    var block = new Block(blockHeader, transactions);
                    var serializedBlockHeader = JsonConvert.SerializeObject(block.BlockHeader);
                    var blockHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedBlockHeader));

                    block.BlockHeader.BlockHash = blockHash;
                    block.BlockHeader.ProducerSignature = SignatureHelper.SignHash(_nodeConfigurations.ActivePrivateKey, blockHash);

                    _logger.LogInformation($"Produced Block -> sequence number: {currentSequenceNumber}, blockhash: {HashHelper.ByteArrayToFormattedHexaString(blockHash)}, previousBlockhash: {HashHelper.ByteArrayToFormattedHexaString(previousBlockhash)}");

                    return block;
                }
                catch (Exception e)
                {
                    i++;
                    _logger.LogCritical($"Failed try #{i} to produce block");
                    _logger.LogDebug(e.ToString());
                    throw e;
                }
            }
            throw new OperationCanceledException("Failed to produce block");
        }

        private async Task CancelProposalTransactionIfExists()
        {
            try
            {
                var proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, _sidechainPool.ClientAccountName);
                if (proposal == null) return;
                
                _logger.LogInformation("Canceling existing proposal...");
                await _mainchainService.CancelTransaction(_nodeConfigurations.AccountName, proposal.ProposalName);
            }
            catch (ApiErrorException apiException)
            {
                _logger.LogCritical($"Unable to cancel existing proposal with error: {apiException?.error?.name}");
            }
        }

        private async Task ProposeBlock(Block block)
        {
            var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
            var blockheaderEOS = block.BlockHeader.ConvertToEosObject();

            var addBlockTransaction = await _mainchainService.AddBlock(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockheaderEOS);

            await TryProposeTransaction(requestedApprovals, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
            await _blockSender.SendBlockToSidechainMembers(_sidechainPool, block.ConvertToProto(), _endPoint);
            await TryVerifyAndExecuteTransaction(_nodeConfigurations.AccountName);
        }

        private async Task TryProposeTransaction(List<string> requestedApprovals, string blockHash)
        {
            while ((_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                try
                {
                    var proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, _sidechainPool.ClientAccountName);
                    if (proposal != null) return;
                    await _mainchainService.ProposeBlockVerification(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, requestedApprovals, blockHash);
                    await Task.Delay(60);
                }
                catch (ApiErrorException)
                {
                    _logger.LogCritical("Unable to propose transaction.");
                    await Task.Delay(100);
                }
            }
        }

        private async Task TryVerifyAndExecuteTransaction(string proposer)
        {
            while ((_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                try
                {
                    var proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, _sidechainPool.ClientAccountName);
                    var approvals = (await _mainchainService.RetrieveApprovals(proposer)).FirstOrDefault();

                    if (proposal != null && approvals?.ProvidedApprovals?.Where(a => a.PermissionLevel.actor == _nodeConfigurations.AccountName).FirstOrDefault() == null)
                    {
                        await TryApproveTransaction(proposal);
                    }
                    else if (approvals?.ProvidedApprovals?.Count >= approvals?.RequestedApprovals?.Count + 1)
                    {
                        await _mainchainService.ExecuteTransaction(proposer, proposal.ProposalName, _nodeConfigurations.AccountName);
                        _logger.LogInformation("Executed block verification");
                        return;
                    }

                    await Task.Delay(100);
                }
                catch (ApiErrorException)
                {
                    _logger.LogCritical("Unable to execute proposed transaction, number of required approvals might not have been reached");
                    await Task.Delay(100);
                }
            }
            _logger.LogCritical("Unable to approve and execute transaction during allowed time");
        }

        private async Task TryApproveTransaction(TransactionProposal proposal)
        {
            try
            {
                await _mainchainService.ApproveTransaction(_nodeConfigurations.AccountName, proposal.ProposalName, _nodeConfigurations.AccountName, proposal.TransactionHash);
            }
            catch (ApiErrorException apiException)
            {
                _logger.LogCritical($"Unable to approve transaction with error: {apiException?.error?.name}");
            }
        }

        private async Task BuildChain()
        {
            _logger.LogDebug("Building chain.");
            var task = _chainBuilder.Start(_sidechainPool);

            while (task.Task.Status == TaskStatus.Running)
            {
                await Task.Delay(50);
            }

            try
            {
                await _mainchainService.NotifyReady(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
            }
            catch (ApiErrorException)
            {
                _logger.LogInformation("Already notified ready.");
            }
        }

        private async Task UpdatePeers()
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var producersInPool = producersInTable.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    NewlyJoined = true
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
        }
    }
}