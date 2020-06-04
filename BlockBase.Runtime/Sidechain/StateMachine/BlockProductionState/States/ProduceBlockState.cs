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
using EosSharp.Core.Exceptions;
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

        private BlockRequestsHandler _blockSender;

        private BlockheaderTable _lastSubmittedBlockHeader;

        private Block _builtBlock;
        private string _blockHash;

        private bool _hasCheckedDbForOldBlock;
        private bool _hasStoredBlockLocally;
        private bool _hasProducedBlock;
        private bool _hasSignedBlock;
        private bool _hasEnoughSignatures;
        private bool _hasBroadcastedBlock;

        private bool _hasBlockBeenVerified;

        private int _numOfBlockBroadcasts;

        private const int MAX_NUMBER_OF_BLOCK_BROADCASTS = 3;

        private (byte[] packedTransaction, List<string> signatures) _packedTransactionAndSignatures;


        public ProduceBlockState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, BlockRequestsHandler blockSender) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _blockSender = blockSender;
            _hasCheckedDbForOldBlock = false;
            _hasStoredBlockLocally = false;
            _hasProducedBlock = false;
            _hasSignedBlock = false;
            _hasEnoughSignatures = false;
            _hasBroadcastedBlock = false;
            _hasBlockBeenVerified = false;
            _packedTransactionAndSignatures = (null, null);
        }

        protected override async Task DoWork()
        {
            _logger.LogDebug("Producing block.");

            if (!_hasCheckedDbForOldBlock)
            {
                var checkIfBlockInDb = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(
                    _sidechainPool.ClientAccountName,
                    _builtBlock.BlockHeader.SequenceNumber,
                    _builtBlock.BlockHeader.SequenceNumber)).FirstOrDefault();

                if (checkIfBlockInDb != null)
                {
                    var blockInDbHash = HashHelper.ByteArrayToFormattedHexaString(checkIfBlockInDb.BlockHeader.BlockHash);
                    await _mongoDbProducerService.RemoveBlockFromDatabaseAsync(_sidechainPool.ClientAccountName, blockInDbHash);
                }
                _hasCheckedDbForOldBlock = true;
            }
            if (!_hasStoredBlockLocally)
            {
                await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(_builtBlock, _sidechainPool.ClientAccountName);
                _hasStoredBlockLocally = true;
            }


            if (!_hasProducedBlock)
                await TryAddBlock(_builtBlock.BlockHeader);

            if (_hasProducedBlock && !_hasSignedBlock)
                await TryAddVerifyTransaction(_blockHash);

            if (_hasProducedBlock && _hasSignedBlock && !_hasBroadcastedBlock)
            {
                await _blockSender.SendBlockToSidechainMembers(_sidechainPool, _builtBlock.ConvertToProto(), _networkConfigurations.GetEndPoint());

                if (_numOfBlockBroadcasts++ >= MAX_NUMBER_OF_BLOCK_BROADCASTS) ;
                _hasBroadcastedBlock = true;
            }
            if (_hasProducedBlock && _hasSignedBlock && _hasBroadcastedBlock && _hasEnoughSignatures)
            {
                await TryBroadcastVerifyTransaction(_packedTransactionAndSignatures.packedTransaction, _packedTransactionAndSignatures.signatures);
            }

            //delete this commented code?
            //await TryVerifyAndExecuteTransaction(_nodeConfigurations.AccountName);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(
                _contractStateTable.ProductionTime
                && _producerList.Any(p => p.Key == _nodeConfigurations.AccountName)
                && _currentProducer.Producer == _nodeConfigurations.AccountName);

            //TODO rpinto - check if he has contacts
            //if he has no contacts there shouldn't be no condition to continue
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_currentProducer.Producer == _nodeConfigurations.AccountName && _hasProducedBlock && _hasSignedBlock && _hasBroadcastedBlock && _hasEnoughSignatures && _hasBlockBeenVerified)
                return Task.FromResult((true, typeof(StartState).Name));

            else return Task.FromResult((false, typeof(StartState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            var hasPreviousBlockBeenProducedByThisProducer = 
                (_currentProducer.Producer == _nodeConfigurations.AccountName && _currentProducer.HasProducedBlock)
                && (_lastSubmittedBlockHeader?.IsVerified ?? false);

            if (hasPreviousBlockBeenProducedByThisProducer)
            {
                if(_builtBlock != null && _builtBlock.BlockHeader != null)
                {
                    _logger.LogInformation($"Produced Block -> sequence number: {_builtBlock.BlockHeader.SequenceNumber}, blockhash: {HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.BlockHash)}, previousBlockhash: {HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.PreviousBlockHash)}");
                }
                
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        protected override async Task UpdateStatus()
        {
            _delay = TimeSpan.FromMilliseconds(500);

            var contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            var currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);
            var lastSubmittedBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            var hasPreviousBlockBeenProducedByThisProducer = 
                (currentProducer.Producer == _nodeConfigurations.AccountName && currentProducer.HasProducedBlock)
                && (lastSubmittedBlockHeader?.IsVerified ?? false);

            //no work to do
            if(hasPreviousBlockBeenProducedByThisProducer)
            {
                _currentProducer = currentProducer;
                _lastSubmittedBlockHeader = lastSubmittedBlockHeader;
                return;
            } 


            var blockHashAndSequenceNumber = CalculatePreviousBlockHashAndSequenceNumber(lastSubmittedBlockHeader);
            var blockHeader = CreateBlockHeader(blockHashAndSequenceNumber.previousBlockhash, blockHashAndSequenceNumber.sequenceNumber);
            var transactionsToIncludeInBlock = await GetTransactionsToIncludeInBlock(blockHeader.ConvertToProto().ToByteArray().Count());

            var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
            var requiredKeys = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.PublicKey).Distinct().ToList();



            Block builtBlock = _builtBlock;
            string blockHash = null;

            if (_builtBlock == null)
            {
                builtBlock = BuildBlock(blockHeader, transactionsToIncludeInBlock);
                blockHash = HashHelper.ByteArrayToFormattedHexaString(builtBlock.BlockHeader.BlockHash);
            }
            else
            {
                blockHash = HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.BlockHash);
            }

            var verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
            var hasSignedBlock = verifySignatureTable.Any(t => t.Account == _nodeConfigurations.AccountName);

            var hasEnoughSignatures = false;
            (byte[] packedTransaction, List<string> signatures) packedTransactionAndSignatures = (null, null);

            if (_hasProducedBlock && _hasSignedBlock)
            {
                hasEnoughSignatures = CheckIfBlockHasMajorityOfSignatures(verifySignatureTable, _blockHash, requestedApprovals.Count, requiredKeys);
                packedTransactionAndSignatures = GetPackedTransactionAndSignatures(verifySignatureTable, _blockHash, requestedApprovals.Count, requiredKeys);
            }

            

            _contractStateTable = contractState;
            _producerList = producerList;
            _currentProducer = currentProducer;
            _lastSubmittedBlockHeader = lastSubmittedBlockHeader;


            _builtBlock = builtBlock;
            _blockHash = blockHash;
            _hasProducedBlock = currentProducer.Producer == _nodeConfigurations.AccountName && currentProducer.HasProducedBlock;
            _hasSignedBlock = hasSignedBlock;
            _hasEnoughSignatures = hasEnoughSignatures;
            _hasBlockBeenVerified = lastSubmittedBlockHeader?.BlockHash == blockHash && (lastSubmittedBlockHeader?.IsVerified ?? false);
            _packedTransactionAndSignatures = packedTransactionAndSignatures;
        }


        private Block BuildBlock(BlockHeader blockHeader, IEnumerable<Transaction> transactions)
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

        private async Task TryAddBlock(BlockHeader blockHeader)
        {
            var blockheaderEOS = blockHeader.ConvertToEosObject();
            var addBlockTransaction = await _mainchainService.AddBlock(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockheaderEOS);
        }

        private async Task TryAddVerifyTransaction(string blockHash)
        {
            await _mainchainService.CreateVerifyBlockTransactionAndAddToContract(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockHash);
        }

        private bool CheckIfBlockHasMajorityOfSignatures(List<VerifySignature> verifySignatureTable, string blockHash, int numberOfProducers, List<string> requiredKeys)
        {
            var verifySignatures = verifySignatureTable?.Where(t => t.BlockHash == blockHash);
            var threshold = (numberOfProducers / 2) + 1;
            var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

            return verifySignatures?.Count() >= threshold;
        }

        private (byte[] packedTransaction, List<string> signatures) GetPackedTransactionAndSignatures(List<VerifySignature> verifySignatureTable, string blockHash, int numberOfProducers, List<string> requiredKeys)
        {
            var verifySignatures = verifySignatureTable?.Where(t => t.BlockHash == blockHash);
            var threshold = (numberOfProducers / 2) + 1;
            var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;
            var signatures = verifySignatures.Select(v => v.Signature).Take(requiredSignatures).ToList();
            var packedTransaction = verifySignatures.FirstOrDefault(v => v.Account == _nodeConfigurations.AccountName)?.PackedTransaction;
            return (packedTransaction, signatures);
        }

        private async Task TryBroadcastVerifyTransaction(byte[] packedTransaction, List<string> signatures)
        {
            await _mainchainService.BroadcastTransactionWithSignatures(packedTransaction, signatures);
            _logger.LogInformation("Executed block verification");
        }
    }
}
