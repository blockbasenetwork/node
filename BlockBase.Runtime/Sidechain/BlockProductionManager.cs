using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
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
        private DateTime _nextTimeToCheckSmartContract;
        private DateTime _previousTimeToCheck;
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
            _nextTimeToCheckSmartContract = DateTime.UtcNow;
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
            // var sidechainName = sidechainPool.SmartContractAccount;
            var databaseName = _sidechainPool.SmartContractAccount;
            try
            {
                while (true)
                {
                    var _timeDiff = (_nextTimeToCheckSmartContract - DateTime.UtcNow).TotalMilliseconds;
                    //_logger.LogDebug($"Next time to check {_nextTimeToCheckSmartContract} | Current {DateTime.UtcNow}");
                    if (_timeDiff < 0)
                    {
                        if (_nextTimeToCheckSmartContract.CompareTo(_previousTimeToCheck) == 0) await Task.Delay(50);
                        try
                        {
                            var currentProducerTable = (await _mainchainService.RetrieveCurrentProducer(_sidechainPool.SmartContractAccount)).SingleOrDefault();

                            if (currentProducerTable != null)
                            {
                                _nextTimeToCheckSmartContract = DateTimeOffset.FromUnixTimeSeconds(currentProducerTable.StartProductionTime).UtcDateTime.AddSeconds(_sidechainPool.BlockTimeDuration);
                                _logger.LogDebug($" Start Mining Time: {DateTimeOffset.FromUnixTimeSeconds(currentProducerTable.StartProductionTime).UtcDateTime} Next time to check smart contract: {_nextTimeToCheckSmartContract}");

                                if (_nextTimeToCheckSmartContract.CompareTo(_previousTimeToCheck) == 0) continue;

                                _previousTimeToCheck = _nextTimeToCheckSmartContract;

                                //_logger.LogDebug($"Next Producer to produce: {currentProducerTable.Producer}");
                                _currentProducingProducerAccountName = currentProducerTable.Producer;

                                var lastValidBlockheaderSmartContractFromLastProduction = await _mainchainService.GetLastValidSubmittedBlockheaderFromLastProduction(_sidechainPool.SmartContractAccount, currentProducerTable.StartProductionTime);
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
            var lastBlock = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.SmartContractAccount);
            uint currentSequenceNumber;
            byte[] previousBlockhash;


            //TODO: this will disappear when we deal with the genesis block generation
            if (lastBlock != null)
            {
                previousBlockhash = HashHelper.FormattedHexaStringToByteArray(lastBlock.BlockHash);
                currentSequenceNumber = lastBlock.SequenceNumber + 1;
            }

            else
            {
                currentSequenceNumber = 1;
                previousBlockhash = HashHelper.Sha256Data(new byte[4]);
            }

            var databaseName = _sidechainPool.SmartContractAccount;
            var allLooseTransactions = await _mongoDbProducerService.RetrieveLastLooseTransactions(databaseName);
            ulong lastSequenceNumber = (await _mongoDbProducerService.LastIncludedTransaction(databaseName))?.SequenceNumber ?? 0;
            var transactions = new List<Transaction>();

            foreach(var looseTransaction in allLooseTransactions)
            {
                if(looseTransaction.SequenceNumber != lastSequenceNumber+1) break;
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

            var addBlockTransaction = await _mainchainService.AddBlock(_sidechainPool.SmartContractAccount, _nodeConfigurations.AccountName, blockheaderEOS);
            //_logger.LogDebug($"Block Producer: Add Block Transaction {addBlockTransaction}");
                                    
            var proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);
            if(proposal != null) await _mainchainService.CancelTransaction(_nodeConfigurations.AccountName,  EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);
                                    
            var proposedTransaction = await _mainchainService.ProposeBlockVerification(_sidechainPool.SmartContractAccount, _nodeConfigurations.AccountName, requestedApprovals, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
            //_logger.LogDebug($"Block Producer: Proposed Verify Block Transaction {proposedTransaction}");

            //_logger.LogDebug("Block Producer: Approving and Executing transaction");
            proposal = await _mainchainService.RetrieveProposal(_nodeConfigurations.AccountName, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);
            await _mainchainService.ApproveTransaction(_nodeConfigurations.AccountName, proposal.ProposalName, _nodeConfigurations.AccountName, proposal.TransactionHash);
            await _blockSender.SendBlockToSidechainMembers(_sidechainPool, block.ConvertToProto(), _endPoint);

            if (_sidechainPool.ProducersInPool.Count() == 1) 
                await _mainchainService.ExecuteTransaction(_nodeConfigurations.AccountName, proposal.ProposalName, _nodeConfigurations.AccountName);
        }

        private async Task BuildChain()
        {
            _logger.LogDebug("Building chain.");
            var task = _chainBuilder.Execute();
            task.Wait();

            try
            {
                await _mainchainService.NotifyReady(_sidechainPool.SmartContractAccount, _nodeConfigurations.AccountName);
            }
            catch (ApiErrorException)
            {
                _logger.LogInformation("Already notified ready.");
            }
        }
    }
}