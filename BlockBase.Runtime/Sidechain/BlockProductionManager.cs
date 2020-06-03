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
using Google.Protobuf;
using BlockBase.Domain.Enums;

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
            if(TaskContainer != null) TaskContainer.Stop();
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
                            //retrieving producer may fail
                            var currentProducerTable = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);

                            if (currentProducerTable != null)
                            {
                                _nextTimeToCheckSmartContract = currentProducerTable.StartProductionTime + _sidechainPool.BlockTimeDuration;

                                if (_nextTimeToCheckSmartContract == _previousTimeToCheck) continue;
                                _logger.LogDebug($"StartProductionTime: {currentProducerTable.StartProductionTime}");
                                _logger.LogDebug($" Start Production Time: {DateTimeOffset.FromUnixTimeSeconds(currentProducerTable.StartProductionTime).UtcDateTime} Next time to check smart contract: {DateTimeOffset.FromUnixTimeSeconds(_nextTimeToCheckSmartContract).UtcDateTime}");

                                _previousTimeToCheck = _nextTimeToCheckSmartContract;
                                _currentProducingProducerAccountName = currentProducerTable.Producer;


                                //has a while loop inside that may fail
                                await CheckIfBlockHeadersInSmartContractAreUpdated(currentProducerTable.StartProductionTime);

                                //retrieving last valid block header may fail
                                var lastValidBlockheaderSmartContract = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
                                
                                //trying to sync databases section
                                if (lastValidBlockheaderSmartContract != null)
                                {
                                    
                                    if (!await _mongoDbProducerService.SynchronizeDatabaseWithSmartContract(databaseName, lastValidBlockheaderSmartContract.BlockHash, currentProducerTable.StartProductionTime) && _sidechainPool.ProducerType != ProducerTypeEnum.Validator)
                                    {
                                        _logger.LogDebug("Producer not up to date, building chain.");

                                        //TODO rpinto - does the provider have enough time to build the chain before being banned?
                                        await BuildChain();
                                    }

                                    if (!await _mongoDbProducerService.IsBlockConfirmed(databaseName, lastValidBlockheaderSmartContract.BlockHash))
                                    {
                                        _logger.LogDebug("Confirming block.");
                                        var transactions = await _mongoDbProducerService.GetBlockTransactionsAsync(databaseName, lastValidBlockheaderSmartContract.BlockHash);
                                        //_sidechainDatabaseManager.ExecuteBlockTransactions(transactions);
                                        await _mongoDbProducerService.ConfirmBlock(databaseName, lastValidBlockheaderSmartContract.BlockHash);
                                    }
                                }

                                //TODO rpinto - the _currentProducingProducerAccountName and currentProducerTable may have been fetched way before
                                //this isn't accounted here
                                if (_currentProducingProducerAccountName == _nodeConfigurations.AccountName && !currentProducerTable.HasProducedBlock)
                                {
                                    _logger.LogDebug("Producing block.");
                                    var block = await ProduceBlock();
                                    var checkIfBlockInDb = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(databaseName, block.BlockHeader.SequenceNumber, block.BlockHeader.SequenceNumber)).FirstOrDefault();
                                    if (checkIfBlockInDb != null)
                                    {
                                        var blockInDbHash = HashHelper.ByteArrayToFormattedHexaString(checkIfBlockInDb.BlockHeader.BlockHash);
                                        await _mongoDbProducerService.RemoveBlockFromDatabaseAsync(databaseName, blockInDbHash);
                                    }
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



                    var blockHeader = new BlockHeader()
                    {
                        Producer = _nodeConfigurations.AccountName,
                        BlockHash = new byte[0],
                        PreviousBlockHash = previousBlockhash,
                        SequenceNumber = currentSequenceNumber,
                        Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                        TransactionCount = 0,
                        ProducerSignature = "",
                        MerkleRoot = new byte[32]
                    };

                    var transactions = await GetTransactionsToIncludeInBlock(blockHeader.ConvertToProto().ToByteArray().Count());

                    blockHeader.TransactionCount = (uint)transactions.Count();
                    blockHeader.MerkleRoot = MerkleTreeHelper.CalculateMerkleRootHash(transactions.Select(t => t.TransactionHash).ToList());

                    var block = new Block(blockHeader, transactions);
                    var blockBytes = block.ConvertToProto().ToByteArray().Count();
                    block.BlockHeader.BlockSizeInBytes = Convert.ToUInt64(blockBytes);
                    
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

        private async Task<IList<Transaction>> GetTransactionsToIncludeInBlock(int blockHeaderSizeInBytes)
        {
            var transactionsDatabaseName = _sidechainPool.ClientAccountName;
            var allLooseTransactions = await _mongoDbProducerService.RetrieveTransactionsInMempool(transactionsDatabaseName);
            ulong lastSequenceNumber = (await _mongoDbProducerService.LastIncludedTransaction(transactionsDatabaseName))?.SequenceNumber ?? 0;
            var transactions = new List<Transaction>();
            uint sizeInBytes = 0;

            foreach (var looseTransaction in allLooseTransactions)
            {
                if (looseTransaction.SequenceNumber != lastSequenceNumber + 1) break;
                var transactionSize = looseTransaction.ConvertToProto().ToByteArray().Count();
                _logger.LogDebug("transaction size in bytes " + _sidechainPool.BlockSizeInBytes);
                if ((sizeInBytes + blockHeaderSizeInBytes + transactionSize) > _sidechainPool.BlockSizeInBytes) break;
                sizeInBytes += (uint)(transactionSize);
                lastSequenceNumber = looseTransaction.SequenceNumber;
                transactions.Add(looseTransaction);
                _logger.LogDebug($"Including transaction {lastSequenceNumber}");
            }
            return transactions;
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

        private async Task CheckIfBlockHeadersInSmartContractAreUpdated(long currentStartProductionTime)
        {
            while (true)
            {
                var lastSubmittedBlock = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
                if (lastSubmittedBlock == null || lastSubmittedBlock.IsVerified || lastSubmittedBlock.Timestamp > currentStartProductionTime) break;
                await Task.Delay(50);
            }
        }

        private async Task ProposeBlock(Block block)
        {
            var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
            var requiredKeys = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.PublicKey).Distinct().ToList();
            var blockHash = HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash);

            await TryAddBlock(block.BlockHeader);
            //await TryProposeTransaction(requestedApprovals, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
            await TryAddVerifyTransaction(blockHash);
            await _blockSender.SendBlockToSidechainMembers(_sidechainPool, block.ConvertToProto(), _endPoint);
            await TryBroadcastVerifyTransaction(blockHash, requestedApprovals.Count, requiredKeys);
            //await TryVerifyAndExecuteTransaction(_nodeConfigurations.AccountName);
        }

        private async Task TryAddBlock(BlockHeader blockHeader)
        {
            var blockheaderEOS = blockHeader.ConvertToEosObject();
            BlockheaderTable blockFromTable = new BlockheaderTable();
            while (blockHeader.SequenceNumber != blockFromTable.SequenceNumber && (_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                try
                {
                    var addBlockTransaction = await _mainchainService.AddBlock(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockheaderEOS);
                    await Task.Delay(500);
                }
                catch (ApiErrorException exception)
                {
                    _logger.LogCritical($"Unable to add block with error: {exception.error.name}");
                    await Task.Delay(100);
                }

                //TODO rpinto - this may fail. Why isn't it inside the try clause
                blockFromTable = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
            }
        }

        private async Task TryAddVerifyTransaction(string blockHash)
        {
            var verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
            var ownSignature = verifySignatureTable.FirstOrDefault(t => t.Account == _nodeConfigurations.AccountName);

            while (ownSignature?.BlockHash != blockHash && (_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                try
                {
                    await _mainchainService.CreateVerifyBlockTransactionAndAddToContract(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockHash);
                    await Task.Delay(500);
                }
                catch (ApiErrorException)
                {
                    _logger.LogCritical("Unable to add verify transaction");
                    await Task.Delay(100);
                }

                //TODO rpinto - this may fail. Why isn't it inside the try clause
                verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
                ownSignature = verifySignatureTable.FirstOrDefault(t => t.Account == _nodeConfigurations.AccountName);
            }
        }

        private async Task TryBroadcastVerifyTransaction(string blockHash, int numberOfProducers, List<string> requiredKeys)
        {
            while ((_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                try
                {
                    var verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
                    var verifySignatures = verifySignatureTable?.Where(t => t.BlockHash == blockHash);
                    var threshold = (numberOfProducers / 2) + 1;
                    var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

                    if (verifySignatures?.Count() >= threshold)
                    {
                        var signatures = verifySignatures.Select(v => v.Signature).Take(requiredSignatures).ToList();
                        var packedTransaction = verifySignatures.FirstOrDefault(v => v.Account == _nodeConfigurations.AccountName).PackedTransaction;
                        _logger.LogDebug($"Broadcasting transaction with {signatures.Count} signatures");

                        await _mainchainService.BroadcastTransactionWithSignatures(packedTransaction, signatures);
                        _logger.LogInformation("Executed block verification");
                        return;
                    }

                    await Task.Delay(100);
                }
                catch (ApiErrorException ex)
                {
                    _logger.LogCritical($"Unable to broadcast verify transaction: {ex.error.name}");
                    await Task.Delay(100);
                }
            }
            _logger.LogCritical("Unable to broadcast verify transaction during allowed time");
        }

        private async Task BuildChain()
        {
            _logger.LogDebug("Building chain.");
            var task = _chainBuilder.Start(_sidechainPool);

            while (task.Task.Status == TaskStatus.Running)
            {
                await Task.Delay(50);
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
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    NewlyJoined = true
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
        }

    }
}