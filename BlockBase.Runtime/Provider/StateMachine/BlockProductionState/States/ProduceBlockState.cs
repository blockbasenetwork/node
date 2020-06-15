using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils.Crypto;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.Provider.StateMachine.BlockProductionState.States
{
    //TODO rpinto - before starting to produce a block some time should be given to make sure all other nodes catch up
    public class ProduceBlockState : ProviderAbstractState<StartState, EndState>
    {

        private IMainchainService _mainchainService;
        private ContractStateTable _contractState;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;

        private NetworkConfigurations _networkConfigurations;
        private List<ProducerInTable> _producerList;
        private CurrentProducerTable _currentProducer;
        private SidechainPool _sidechainPool;

        private BlockRequestsHandler _blockSender;

        private Block _builtBlock;
        private string _blockHash;

        private bool _hasProviderBuiltNewBlock;
        private bool _hasCheckedDbForOldBlock;
        private bool _hasStoredBlockLocally;
        private bool _hasSignedBlock;
        private bool _hasEnoughSignatures;

        private bool _hasBlockBeenVerified;


        private const int MAX_NUMBER_OF_BLOCK_BROADCASTS = 3;

        private (byte[] packedTransaction, List<string> signatures) _packedTransactionAndSignatures;


        public ProduceBlockState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, BlockRequestsHandler blockSender) : base(logger, sidechainPool)
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
            _hasProviderBuiltNewBlock = false;
            _hasSignedBlock = false;
            _hasEnoughSignatures = false;
            _hasBlockBeenVerified = false;
            _packedTransactionAndSignatures = (null, null);
        }

        protected override async Task DoWork()
        {

            if (!_hasProviderBuiltNewBlock)
            {
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
            }



            if (!_hasProviderBuiltNewBlock)
                await TryAddBlock(_builtBlock.BlockHeader);

            if (_hasProviderBuiltNewBlock && !_hasSignedBlock)
                await TryAddVerifyTransaction(_blockHash);

            if (_hasProviderBuiltNewBlock && _hasSignedBlock && !_hasEnoughSignatures)
            {
                _logger.LogDebug("Sending block to other producers");
                await _blockSender.SendBlockToSidechainMembers(_sidechainPool, _builtBlock.ConvertToProto(), _networkConfigurations.GetEndPoint());
            }

            if (_hasProviderBuiltNewBlock && _hasSignedBlock && _hasEnoughSignatures && !_hasBlockBeenVerified)
            {
                await TryBroadcastVerifyTransaction(_packedTransactionAndSignatures.packedTransaction, _packedTransactionAndSignatures.signatures);
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractState == null || _currentProducer == null) return Task.FromResult(false);
            return Task.FromResult(
                _contractState.ProductionTime && _currentProducer.Producer == _nodeConfigurations.AccountName);

            //TODO rpinto - check if he has contacts
            //if he has no contacts there shouldn't be no condition to continue
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_hasBlockBeenVerified)
                return Task.FromResult((true, typeof(StartState).Name));

            else return Task.FromResult((false, typeof(StartState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            if (_hasBlockBeenVerified) return Task.FromResult(true);
            return Task.FromResult(false);
        }

        protected override async Task UpdateStatus()
        {
            _delay = TimeSpan.FromMilliseconds(500);

            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if (_contractState == null) return;
            if (_producerList == null) return;
            if(_currentProducer == null) return;


            var lastSubmittedBlockHeader = await _mainchainService.GetLastSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);

            _hasProviderBuiltNewBlock = false;
            if (lastSubmittedBlockHeader != null
                && _currentProducer.Producer == _nodeConfigurations.AccountName
                && _currentProducer.HasProducedBlock
                && _currentProducer.StartProductionTime <= lastSubmittedBlockHeader.Timestamp)
            {
                _hasProviderBuiltNewBlock = true;
            }


            if (_hasProviderBuiltNewBlock && lastSubmittedBlockHeader.IsVerified)
            {
                _hasBlockBeenVerified = true;
            }

            //block has been produced by producer - no more updating to do
            if (_hasBlockBeenVerified)
            {
                if (_builtBlock != null && _builtBlock.BlockHeader != null)
                {
                    _logger.LogInformation($"Successfully produced Block -> sequence number: {_builtBlock.BlockHeader.SequenceNumber}, blockhash: {HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.BlockHash)}, previousBlockhash: {HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.PreviousBlockHash)}");
                }
                return;
            }

            // _builtBlock and _blockHash are set only once
            if (!_hasProviderBuiltNewBlock && (_builtBlock == null || _builtBlock.BlockHeader.Timestamp < (ulong)_currentProducer.StartProductionTime))
            {
                var lastValidSubmittedBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement);
                var blockHashAndSequenceNumber = CalculatePreviousBlockHashAndSequenceNumber(lastValidSubmittedBlockHeader);
                var blockHeader = CreateBlockHeader(blockHashAndSequenceNumber.previousBlockhash, blockHashAndSequenceNumber.sequenceNumber, lastValidSubmittedBlockHeader?.LastTransactionSequenceNumber);
                var transactionsToIncludeInBlock = await GetTransactionsToIncludeInBlock(blockHeader.ConvertToProto().ToByteArray().Count(), lastValidSubmittedBlockHeader?.LastTransactionSequenceNumber ?? 0);
                _builtBlock = BuildBlock(blockHeader, transactionsToIncludeInBlock);
                _blockHash = HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.BlockHash);

                _hasCheckedDbForOldBlock = false;
                _hasStoredBlockLocally = false;

                _logger.LogInformation($"Proposed Block -> sequence number: {_builtBlock.BlockHeader.SequenceNumber}, blockhash: {HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.BlockHash)}, previousBlockhash: {HashHelper.ByteArrayToFormattedHexaString(_builtBlock.BlockHeader.PreviousBlockHash)}, timestamp: {_builtBlock.BlockHeader.Timestamp}");
            }



            var verifySignatureTable = await _mainchainService.RetrieveVerifySignatures(_sidechainPool.ClientAccountName);
            _hasSignedBlock = verifySignatureTable.Any(t => t.Account == _nodeConfigurations.AccountName);


            if (_hasProviderBuiltNewBlock && _hasSignedBlock && !_hasEnoughSignatures)
            {
                var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
                var requiredKeys = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.PublicKey).Distinct().ToList();

                _hasEnoughSignatures = CheckIfBlockHasMajorityOfSignatures(verifySignatureTable, _blockHash, requestedApprovals.Count, requiredKeys);
                _packedTransactionAndSignatures = GetPackedTransactionAndSignatures(verifySignatureTable, _blockHash, requestedApprovals.Count, requiredKeys);
            }
        }

        private Block BuildBlock(BlockHeader blockHeader, IEnumerable<Transaction> transactions)
        {
            blockHeader.TransactionCount = (uint)transactions.Count();
            blockHeader.MerkleRoot = MerkleTreeHelper.CalculateMerkleRootHash(transactions.Select(t => t.TransactionHash).ToList());
            blockHeader.LastTransactionSequenceNumber = transactions.Any() ? transactions.OrderBy(t => t.SequenceNumber).Last().SequenceNumber : blockHeader.LastTransactionSequenceNumber;

            var block = new Block(blockHeader, transactions.ToList());
            var blockBytes = block.ConvertToProto().ToByteArray().Count();
            block.BlockHeader.BlockSizeInBytes = Convert.ToUInt64(blockBytes);

            var serializedBlockHeader = JsonConvert.SerializeObject(block.BlockHeader);
            var blockHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedBlockHeader));

            block.BlockHeader.BlockHash = blockHash;
            block.BlockHeader.ProducerSignature = SignatureHelper.SignHash(_nodeConfigurations.ActivePrivateKey, blockHash);

            return block;
        }


        private async Task<IList<Transaction>> GetTransactionsToIncludeInBlock(int blockHeaderSizeInBytes, ulong lastIncludedTransactionSequenceNumber)
        {
            var transactionsDatabaseName = _sidechainPool.ClientAccountName;
            var allLooseTransactions = await _mongoDbProducerService.RetrieveTransactionsInMempool(transactionsDatabaseName);
            var transactions = new List<Transaction>();
            uint sizeInBytes = 0;

            foreach (var looseTransaction in allLooseTransactions)
            {
                if (looseTransaction.SequenceNumber != lastIncludedTransactionSequenceNumber + 1) break;
                var transactionSize = looseTransaction.ConvertToProto().ToByteArray().Count();
                _logger.LogDebug("transaction size in bytes " + _sidechainPool.BlockSizeInBytes);
                if ((sizeInBytes + blockHeaderSizeInBytes + transactionSize) > _sidechainPool.BlockSizeInBytes) break;
                sizeInBytes += (uint)(transactionSize);
                lastIncludedTransactionSequenceNumber = looseTransaction.SequenceNumber;
                transactions.Add(looseTransaction);
                _logger.LogDebug($"Including transaction {lastIncludedTransactionSequenceNumber}");
            }
            return transactions;
        }

        private BlockHeader CreateBlockHeader(byte[] previousBlockhash, ulong sequenceNumber, ulong? lastTransactionSequenceNumber)
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
                MerkleRoot = new byte[32],
                LastTransactionSequenceNumber = lastTransactionSequenceNumber ?? 0
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
