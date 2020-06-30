using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Helpers;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.Provider.StateMachine.HistoryValidation.States
{
    public class ValidateHistoryState : AbstractState<StartState, EndState>
    {

        private IMainchainService _mainchainService;
        private ContractStateTable _contractState;
        private IMongoDbProducerService _mongoDbProducerService;
        private NodeConfigurations _nodeConfigurations;

    
        private IList<ProducerInTable> _producerList;
        private IList<MappedHistoryValidation> _historyValidations;
        private SidechainPool _sidechainPool;
        private string _blockByteInHex;
        private string _blockHashToValidate;
        private EosSharp.Core.Api.v1.Transaction _transaction;
        private MappedHistoryValidation _currentProducerHistoryEntry;

        private bool _hasSubmittedBlockByte;
        private bool _hasSignedBlockByte;
        private bool _hasEnoughSignatures;
        private bool _hasToSubmitBlockByte;
        private bool _blockHashToValidateHasChanged;
        private IDictionary<string, string> _blockBytesPerValidationEntryAccount;

        private (byte[] packedTransaction, List<string> signatures) _packedTransactionAndSignatures;


        public ValidateHistoryState(ILogger logger, IMainchainService mainchainService,
            IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool,
            NodeConfigurations nodeConfigurations) : base(logger)
        {
            _logger = logger;
            _mainchainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
            _packedTransactionAndSignatures = (null, null);
            _blockBytesPerValidationEntryAccount = new Dictionary<string, string>();
        }

        protected override async Task DoWork()
        {

            if (_hasToSubmitBlockByte && !_hasSubmittedBlockByte)
                await TryAddBlockByte(_blockByteInHex, _blockHashToValidate);

            if (_hasToSubmitBlockByte && _hasSubmittedBlockByte && !_hasSignedBlockByte)
                await TryAddHistorySignature(_nodeConfigurations.AccountName, _blockByteInHex, _transaction);

            if (_hasToSubmitBlockByte && _hasSubmittedBlockByte && _hasEnoughSignatures)
                await TryBroadcastVerifyTransaction(_packedTransactionAndSignatures.packedTransaction, _packedTransactionAndSignatures.signatures);

            foreach (var blockBytePerValidationEntryAccount in _blockBytesPerValidationEntryAccount)
            {
                var producerHistoryValidation = _historyValidations.Where(t => t.Account == blockBytePerValidationEntryAccount.Key).SingleOrDefault();
                if (producerHistoryValidation != null &&
                    !producerHistoryValidation.SignedProducers.Contains(_nodeConfigurations.AccountName) &&
                    producerHistoryValidation.BlockByteInHexadecimal == blockBytePerValidationEntryAccount.Value)
                {
                    await TryAddHistorySignature(blockBytePerValidationEntryAccount.Key, producerHistoryValidation.BlockByteInHexadecimal, producerHistoryValidation.Transaction);
                }
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if (_contractState == null || _producerList == null || _historyValidations == null) return Task.FromResult(false);
            return Task.FromResult(_producerList.Any(p => p.Key == _nodeConfigurations.AccountName));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_blockHashToValidateHasChanged || (!_historyValidations.Any(t => t.BlockByteInHexadecimal != "" && !t.SignedProducers.Contains(_nodeConfigurations.AccountName)) && _currentProducerHistoryEntry == null))
                return Task.FromResult((true, typeof(StartState).Name));

            else return Task.FromResult((false, typeof(StartState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            if (_historyValidations.Any(t => !t.SignedProducers.Contains(_nodeConfigurations.AccountName))) return Task.FromResult(false);
            return Task.FromResult(_currentProducerHistoryEntry == null);
        }

        protected override async Task UpdateStatus()
        {
            _delay = TimeSpan.FromMilliseconds(500);

            _contractState = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _historyValidations = await _mainchainService.RetrieveHistoryValidation(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if (_contractState == null) return;
            if (_producerList == null) return;
            if (_historyValidations == null || !_historyValidations.Any()) return;

            _currentProducerHistoryEntry = _historyValidations.Where(e => e.Account == _nodeConfigurations.AccountName).SingleOrDefault();

            if (_blockHashToValidate != null && _currentProducerHistoryEntry != null)
            {
                _blockHashToValidateHasChanged = _blockHashToValidate == _currentProducerHistoryEntry.BlockHash ? false : true;
            }
            else if (_blockHashToValidate == null && _currentProducerHistoryEntry != null)
            {
                _blockHashToValidate = _currentProducerHistoryEntry.BlockHash;
            }

            _hasToSubmitBlockByte = _currentProducerHistoryEntry != null ? true : false;

            if (_hasToSubmitBlockByte && _blockByteInHex == null)
            {
                _blockByteInHex = await GetBlockByte(_currentProducerHistoryEntry.BlockHash, _sidechainPool.ClientAccountName);
                _logger.LogInformation($"Calculated my validation block byte: {_blockByteInHex}.");
            }

            _hasSubmittedBlockByte = _currentProducerHistoryEntry?.BlockByteInHexadecimal != "" && _currentProducerHistoryEntry?.BlockByteInHexadecimal != null ;

            if (_hasSubmittedBlockByte && !_hasEnoughSignatures)
            {
                _transaction = _currentProducerHistoryEntry.Transaction;

                var requestedApprovals = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).OrderBy(p => p).ToList();
                var requiredKeys = _sidechainPool.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.PublicKey).Distinct().ToList();

                _hasEnoughSignatures = CheckIfBlockByteHasMajorityOfSignatures(_currentProducerHistoryEntry, requestedApprovals.Count, requiredKeys);
                _packedTransactionAndSignatures = GetPackedTransactionAndSignatures(_currentProducerHistoryEntry, _blockByteInHex, requestedApprovals.Count, requiredKeys);
                
            }

            _hasSignedBlockByte = _currentProducerHistoryEntry?.SignedProducers.Any(p => p == _nodeConfigurations.AccountName) ?? false;

            foreach (var historyValidationTable in _historyValidations)
            {
                if (!_blockBytesPerValidationEntryAccount.ContainsKey(historyValidationTable.Account) && historyValidationTable.Account != _nodeConfigurations.AccountName)
                {
                    var blockByte = await GetBlockByte(historyValidationTable.BlockHash, _sidechainPool.ClientAccountName);
                    _logger.LogInformation($"Calculated provider {historyValidationTable.Account} validation block byte: {blockByte}.");
                    _blockBytesPerValidationEntryAccount[historyValidationTable.Account] = blockByte;
                }
            }
        }

        private async Task TryAddBlockByte(string blockByte, string blockHash)
        {
            var addBlockByteTransaction = await _mainchainService.SubmitBlockByte(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, blockByte, blockHash);
            _logger.LogDebug($"Adding block byte {blockByte}.");
        }

        private bool CheckIfBlockByteHasMajorityOfSignatures(MappedHistoryValidation historyValidationTable, int numberOfProducers, List<string> requiredKeys)
        {
            var threshold = (numberOfProducers / 2) + 1;
            var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

            _logger.LogDebug("Checked if block has majority of signatures.");

            return historyValidationTable?.VerifySignatures.Count() >= threshold;
        }

        private (byte[] packedTransaction, List<string> signatures) GetPackedTransactionAndSignatures(MappedHistoryValidation historyValidationTable, string blockHash, int numberOfProducers, List<string> requiredKeys)
        {
            var threshold = (numberOfProducers / 2) + 1;
            var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

            var signatures = historyValidationTable?.VerifySignatures.Take(requiredSignatures).ToList();
            var packedTransaction = historyValidationTable?.PackedTransaction;

            _logger.LogDebug("Get packed transactions and signatures.");
            return (packedTransaction, signatures);
        }

        private async Task TryBroadcastVerifyTransaction(byte[] packedTransaction, List<string> signatures)
        {
            await _mainchainService.BroadcastTransactionWithSignatures(packedTransaction, signatures);
            _logger.LogInformation("Executed history validation");
        }

        private async Task TryAddHistorySignature(string producerToValidade, string blockByteInHexadecimal, EosSharp.Core.Api.v1.Transaction transaction)
        {
            await _mainchainService.SignHistoryValidation(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, producerToValidade, blockByteInHexadecimal, transaction);
            _logger.LogDebug("Added history signature");
        }

        private async Task<string> GetBlockByte(string blockhash, string clientAccountName)
        {
            var block = await _mongoDbProducerService.GetSidechainBlockAsync(clientAccountName, blockhash);

            if (block == null)
            {
                _logger.LogWarning("Producer does not have most current block for history validation.");
                return null;
            }

            var blockHashNumber = BitConverter.ToUInt64(HashHelper.FormattedHexaStringToByteArray(blockhash));
            // logger.LogWarning("Blockhash converted to number: " + blockHashNumber);

            var chosenBlockSequenceNumber = (blockHashNumber % block.BlockHeader.SequenceNumber) + 1;
            // logger.LogWarning($"Current block sequence number {block.BlockHeader.SequenceNumber}, chosen block sequence number {chosenBlockSequenceNumber}");

            var chosenBlock = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(clientAccountName, chosenBlockSequenceNumber, chosenBlockSequenceNumber)).SingleOrDefault();
            if (chosenBlock == null)
            {
                _logger.LogWarning("Producer does not have randomly chosen block for history validation.");
                return null;
            }

            var blockBytes = chosenBlock.ConvertToProto().ToByteArray();
            var byteIndex = (int)(blockHashNumber % (ulong)blockBytes.Count());
            // logger.LogWarning($"Number of block bytes {blockBytes.Count()}, chosen byte{byteIndex}");

            return HashHelper.ByteArrayToFormattedHexaString(new byte[] { blockBytes[byteIndex] });
        }

    }
}
