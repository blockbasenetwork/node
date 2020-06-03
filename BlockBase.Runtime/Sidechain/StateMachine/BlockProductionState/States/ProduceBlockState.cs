using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Utils.Crypto;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    //TODO rpinto - before starting to produce a block some time should be given to make sure all other nodes catch up
    public class ProduceBlockState : AbstractState<StartState, EndState>
    {

        private IMainchainService _mainchainService;
        private ContractStateTable _contractStateTable;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;

        private NetworkConfigurations _networkConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private SidechainPool _sidechainPool;
        private ISidechainDatabasesManager _sidechainDatabaseManager;

        private BlockRequestsHandler _blockSender;

        private BlockheaderTable _lastSubmittedBlockHeader;
        private bool _hasProducedBlock;

        public ProduceBlockState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations,
            ISidechainDatabasesManager sidechainDatabaseManager, BlockRequestsHandler blockSender) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainDatabaseManager = sidechainDatabaseManager;
            _blockSender = blockSender;
            _hasProducedBlock = false;
        }

        protected override async Task DoWork()
        {
            _logger.LogDebug("Producing block.");

            var blockHashAndSequenceNumber = CalculatePreviousBlockHashAndSequenceNumber(_lastSubmittedBlockHeader);
            var blockHeader = CreateBlockHeader(blockHashAndSequenceNumber.previousBlockhash, blockHashAndSequenceNumber.sequenceNumber);
            var transactionsToIncludeInBlock = await GetTransactionsToIncludeInBlock(blockHeader.ConvertToProto().ToByteArray().Count());

            var block = ProduceBlock(blockHeader, transactionsToIncludeInBlock);
            
            var checkIfBlockInDb = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(
                _sidechainPool.ClientAccountName, 
                block.BlockHeader.SequenceNumber, 
                block.BlockHeader.SequenceNumber)).FirstOrDefault();

            if (checkIfBlockInDb != null)
            {
                var blockInDbHash = HashHelper.ByteArrayToFormattedHexaString(checkIfBlockInDb.BlockHeader.BlockHash);
                await _mongoDbProducerService.RemoveBlockFromDatabaseAsync(_sidechainPool.ClientAccountName, blockInDbHash);
            }
            await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(block, _sidechainPool.ClientAccountName);
            
            
            
            //await ProposeBlock(block);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(
                _contractStateTable.ProductionTime
                && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName)
                && _currentProducer.Producer == _nodeConfigurations.AccountName);

            //TODO 
            //if he has no contacts there shouldn't be no condition to continue
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_currentProducer.Producer == _nodeConfigurations.AccountName && _hasProducedBlock
                || !_producerList.Any(p => p.Key == _nodeConfigurations.AccountName)
                || _currentProducer.Producer != _nodeConfigurations.AccountName)
                return Task.FromResult((true, typeof(StartState).Name));

            else return Task.FromResult((false, typeof(StartState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_hasProducedBlock);
        }

        protected override async Task UpdateStatus()
        {
            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);
            var lastSubmittedBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            _contractStateTable = contractState;
            _producerList = producerList;
            _currentProducer = currentProducer;
            _lastSubmittedBlockHeader = lastSubmittedBlockHeader;

            _hasProducedBlock = currentProducer.Producer == _nodeConfigurations.AccountName && currentProducer.HasProducedBlock;

            throw new System.NotImplementedException();
        }


        private Block ProduceBlock(BlockHeader blockHeader, IEnumerable<Transaction> transactions)
        {
            blockHeader.TransactionCount = (uint)transactions.Count();
            blockHeader.MerkleRoot = MerkleTreeHelper.CalculateMerkleRootHash(transactions.Select(t => t.TransactionHash).ToList());

            var block = new Block(blockHeader, transactions.ToList());
            var blockBytes = block.ConvertToProto().ToByteArray().Count();
            block.BlockHeader.BlockSizeInBytes = Convert.ToUInt64(blockBytes);

            var serializedBlockHeader = JsonConvert.SerializeObject(block.BlockHeader);
            var blockHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedBlockHeader));

            block.BlockHeader.BlockHash = blockHash;
            block.BlockHeader.ProducerSignature = SignatureHelper.SignHash(_nodeConfigurations.ActivePrivateKey, blockHash);

            _logger.LogInformation($"Produced Block -> sequence number: {block.BlockHeader.SequenceNumber}, blockhash: {HashHelper.ByteArrayToFormattedHexaString(blockHash)}, previousBlockhash: {HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.PreviousBlockHash)}");

            return block;
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

        private BlockHeader CreateBlockHeader(byte[] previousBlockhash, ulong sequenceNumber)
        {
            return new BlockHeader()
            {
                Producer = _nodeConfigurations.AccountName,
                BlockHash = new byte[0],
                PreviousBlockHash = previousBlockhash,
                SequenceNumber = sequenceNumber,
                Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                TransactionCount = 0,
                ProducerSignature = "",
                MerkleRoot = new byte[32]
            };
        }

        private (byte[] previousBlockhash, ulong sequenceNumber) CalculatePreviousBlockHashAndSequenceNumber(BlockheaderTable lastBlock)
        {
            ulong currentSequenceNumber;
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

            return (previousBlockhash, currentSequenceNumber);
        }

        // private async Task ProposeBlock(Block block)
        // {
        //     var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
        //     var requiredKeys = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.PublicKey).Distinct().ToList();
        //     var blockHash = HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash);

        //     await TryAddBlock(block.BlockHeader);
        //     //await TryProposeTransaction(requestedApprovals, HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash));
        //     await TryAddVerifyTransaction(blockHash);
        //     await _blockSender.SendBlockToSidechainMembers(_sidechainPool, block.ConvertToProto(), _networkConfigurations.GetEndPoint());
        //     await TryBroadcastVerifyTransaction(blockHash, requestedApprovals.Count, requiredKeys);
        //     //await TryVerifyAndExecuteTransaction(_nodeConfigurations.AccountName);
        // }

        // private async Task TryAddBlock(BlockHeader blockHeader)
        // {
        //     var blockheaderEOS = blockHeader.ConvertToEosObject();
        //     BlockheaderTable blockFromTable = new BlockheaderTable();
        //     while (blockHeader.SequenceNumber != blockFromTable.SequenceNumber && (_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        //     {
        //         try
        //         {
        //             var addBlockTransaction = await _mainchainService.AddBlock(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockheaderEOS);
        //             await Task.Delay(500);
        //         }
        //         catch (ApiErrorException exception)
        //         {
        //             _logger.LogCritical($"Unable to add block with error: {exception.error.name}");
        //             await Task.Delay(100);
        //         }

        //         //TODO rpinto - this may fail. Why isn't it inside the try clause
        //         blockFromTable = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
        //     }
        // }

        // private async Task TryAddVerifyTransaction(string blockHash)
        // {
        //     var verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
        //     var ownSignature = verifySignatureTable.FirstOrDefault(t => t.Account == _nodeConfigurations.AccountName);

        //     while (ownSignature?.BlockHash != blockHash && (_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        //     {
        //         try
        //         {
        //             await _mainchainService.CreateVerifyBlockTransactionAndAddToContract(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockHash);
        //             await Task.Delay(500);
        //         }
        //         catch (ApiErrorException)
        //         {
        //             _logger.LogCritical("Unable to add verify transaction");
        //             await Task.Delay(100);
        //         }

        //         //TODO rpinto - this may fail. Why isn't it inside the try clause
        //         verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
        //         ownSignature = verifySignatureTable.FirstOrDefault(t => t.Account == _nodeConfigurations.AccountName);
        //     }
        // }

        // private async Task TryBroadcastVerifyTransaction(string blockHash, int numberOfProducers, List<string> requiredKeys)
        // {
        //     while ((_nextTimeToCheckSmartContract * 1000) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        //     {
        //         try
        //         {
        //             var verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
        //             var verifySignatures = verifySignatureTable?.Where(t => t.BlockHash == blockHash);
        //             var threshold = (numberOfProducers / 2) + 1;
        //             var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

        //             if (verifySignatures?.Count() >= threshold)
        //             {
        //                 var signatures = verifySignatures.Select(v => v.Signature).Take(requiredSignatures).ToList();
        //                 var packedTransaction = verifySignatures.FirstOrDefault(v => v.Account == _nodeConfigurations.AccountName).PackedTransaction;
        //                 _logger.LogDebug($"Broadcasting transaction with {signatures.Count} signatures");

        //                 await _mainchainService.BroadcastTransactionWithSignatures(packedTransaction, signatures);
        //                 _logger.LogInformation("Executed block verification");
        //                 return;
        //             }

        //             await Task.Delay(100);
        //         }
        //         catch (ApiErrorException ex)
        //         {
        //             _logger.LogCritical($"Unable to broadcast verify transaction: {ex.error.name}");
        //             await Task.Delay(100);
        //         }
        //     }
        //     _logger.LogCritical("Unable to broadcast verify transaction during allowed time");
        // }
    }
}
