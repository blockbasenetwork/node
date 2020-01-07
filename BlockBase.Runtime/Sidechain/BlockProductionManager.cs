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

namespace BlockBase.Runtime.Sidechain
{
    public class BlockProductionManager : IThreadableComponent
    {
        public TaskContainer TaskContainer { get; private set; }
        private string _currentProducingProducerAccountName;
        private SidechainPool _sidechainPool;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private string _endPoint;
        private BlockSender _blockSender;
        private ChainBuilder _chainBuilder;
        private long _nextTimeToCheckSmartContract;
        private long _previousTimeToCheck;
        private ILogger _logger;
        private IMongoDbProducerService _mongoDbProducerService;

        //TODO: change this when client specifies database type (MYSQL, SQL, ...)
        private ISidechainDatabasesManager _sidechainDatabaseManager;


        public BlockProductionManager(SidechainPool sidechainPool, NodeConfigurations nodeConfigurations, ILogger logger, INetworkService networkService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, string endPoint, BlockSender blockSender, ISidechainDatabasesManager sidechainDatabaseManager)
        {
            _logger = logger;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _logger.LogDebug("Creating block producer.");
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _endPoint = endPoint;
            _blockSender = blockSender;
            _sidechainDatabaseManager = sidechainDatabaseManager;
            _chainBuilder = new ChainBuilder(logger, sidechainPool, _mongoDbProducerService, _sidechainDatabaseManager, nodeConfigurations, networkService, mainchainService, endPoint);

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
            var databaseName = _sidechainPool.SidechainName;
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
                            var currentProducerTable = (await _mainchainService.RetrieveCurrentProducer(_sidechainPool.SidechainName)).SingleOrDefault();

                            if (currentProducerTable != null)
                            {
                                _nextTimeToCheckSmartContract = currentProducerTable.StartProductionTime + _sidechainPool.BlockTimeDuration;
                                _logger.LogDebug($"StartProductionTime: {currentProducerTable.StartProductionTime}");
                                _logger.LogDebug($" Start Production Time: {DateTimeOffset.FromUnixTimeSeconds(currentProducerTable.StartProductionTime).UtcDateTime} Next time to check smart contract: {DateTimeOffset.FromUnixTimeSeconds(_nextTimeToCheckSmartContract).UtcDateTime}");

                                if (_nextTimeToCheckSmartContract == _previousTimeToCheck) continue;

                                _previousTimeToCheck = _nextTimeToCheckSmartContract;

                                _currentProducingProducerAccountName = currentProducerTable.Producer;

                                var lastValidBlockheaderSmartContractFromLastProduction = await _mainchainService.GetLastValidSubmittedBlockheaderFromLastProduction(_sidechainPool.SidechainName, currentProducerTable.StartProductionTime);
                                if (lastValidBlockheaderSmartContractFromLastProduction != null)
                                {
                                    if (!await _mongoDbProducerService.SynchronizeDatabaseWithSmartContract(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash, currentProducerTable.StartProductionTime))
                                    {
                                        await BuildChain();
                                        continue;
                                    }

                                    if (!await _mongoDbProducerService.IsBlockConfirmed(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash))
                                    {
                                        var transactions = await _mongoDbProducerService.GetBlockTransactionsAsync(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash);
                                        _sidechainDatabaseManager.ExecuteBlockTransactions(transactions);
                                        await _mongoDbProducerService.ConfirmBlock(databaseName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash);
                                    }
                                }

                                if (_currentProducingProducerAccountName == _nodeConfigurations.AccountName)
                                {
                                    var block = await ProduceBlock();
                                    await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(block, databaseName);
                                    await ProposeBlock(block);
                                }
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
            var lastBlock = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.SidechainName);
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

            var databaseName = _sidechainPool.SidechainName;
            var allLooseTransactions = await _mongoDbProducerService.RetrieveLastLooseTransactions(databaseName);
            ulong lastSequenceNumber = (await _mongoDbProducerService.LastIncludedTransaction(databaseName))?.SequenceNumber ?? 0;
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

        private async Task ProposeBlock(Block block)
        {
            var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
            var blockheaderEOS = block.BlockHeader.ConvertToEosObject();

            var addBlockTransaction = await _mainchainService.AddBlock(_sidechainPool.SidechainName, _nodeConfigurations.AccountName, blockheaderEOS);

            var proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);
            if (proposal != null) await _mainchainService.CancelTransaction(_nodeConfigurations.AccountName, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);

            var proposedTransaction = await _mainchainService.ProposeBlockVerification(_sidechainPool.SidechainName, _nodeConfigurations.AccountName, requestedApprovals, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));

            while (proposal == null && (_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);
                if (proposal == null) await Task.Delay(50);
            }
            
            try 
            {
                await _mainchainService.ApproveTransaction(_nodeConfigurations.AccountName, proposal.ProposalName, _nodeConfigurations.AccountName, proposal.TransactionHash);
            }
            catch(ApiErrorException apiException)
            {
                _logger.LogCritical($"Unable to approve transaction with error: {apiException?.error?.name}");
            }
            
            await _blockSender.SendBlockToSidechainMembers(_sidechainPool, block.ConvertToProto(), _endPoint);

            await TryVerifyAndExecuteTransaction(_nodeConfigurations.AccountName, proposal);
        }

        private async Task TryVerifyAndExecuteTransaction(string proposer, TransactionProposal proposal)
        {
            try
            {
                while ((_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    var approvals = _mainchainService.RetrieveApprovals(proposer)?.Result?.FirstOrDefault();
                    if (approvals?.ProvidedApprovals?.Count >= approvals?.RequestedApprovals?.Count + 1)
                    {
                        await _mainchainService.ExecuteTransaction(proposer, proposal.ProposalName, _nodeConfigurations.AccountName);
                        _logger.LogInformation("Executed block verification");
                        return;
                    }
                    await Task.Delay(100);
                }
            }
            catch(ApiErrorException)
            {
                _logger.LogInformation("Unable to execute, proposed transaction might have already been executed");
            }

            try
            {
                _logger.LogInformation("Canceling proposal...");
                await _mainchainService.CancelTransaction(proposer, proposal.ProposalName);
            }
            catch(ApiErrorException)
            {
                _logger.LogCritical("Failed to cancel proposal after failed execution");
            }
        }

        private async Task BuildChain()
        {
            _logger.LogDebug("Building chain.");
            var task = _chainBuilder.Execute();
            task.Wait();

            try
            {
                await _mainchainService.NotifyReady(_sidechainPool.SidechainName, _nodeConfigurations.AccountName);
            }
            catch (ApiErrorException)
            {
                _logger.LogInformation("Already notified ready.");
            }
        }
    }
}