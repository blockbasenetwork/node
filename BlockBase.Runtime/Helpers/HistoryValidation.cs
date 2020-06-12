using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using EosSharp.Core.Exceptions;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Helpers
{
    public class HistoryValidation
    {
        
        public TaskContainer TaskContainer { get; private set; }

        private IMongoDbProducerService _mongoDbProducerService;
        private ILogger _logger;
        private IMainchainService _mainchainService;

        public HistoryValidation(ILogger logger, IMongoDbProducerService mongoDbProducerService, IMainchainService mainchainService)
        {
            _logger = logger;
            _mongoDbProducerService = mongoDbProducerService;
            _mainchainService = mainchainService;
        }

        public TaskContainer StartHistoryValidationTask(string accountName, string blockhash, SidechainPool sidechainPool)
        {
            TaskContainer = TaskContainer.Create(async () => await ProposeHistoryValidationAndTryToExecute(accountName, blockhash, sidechainPool));
            TaskContainer.Start();
            return TaskContainer;
        }

        //TODO rpinto - what does this do and why is it done assynchronously?
        public async Task ProposeHistoryValidationAndTryToExecute(string accountName, string blockhash, SidechainPool sidechainPool)
        {
            var historyTable = await _mainchainService.RetrieveHistoryValidationTable(sidechainPool.ClientAccountName);
            var owner = sidechainPool.ClientAccountName;
            var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
            var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
            var proposalName = firstHalf + "hi" + secondHalf;

            while (historyTable != null && historyTable.Key == accountName && historyTable.BlockHash == blockhash)
            {
                var proposal = await _mainchainService.RetrieveProposal(accountName, proposalName);

                if (proposal == null)
                {
                    var validationAnswer = await GetValidationAnswer(blockhash, owner);

                    try
                    {
                        await _mainchainService.AddBlockByte(owner, accountName, validationAnswer);
                        await _mainchainService.ProposeHistoryValidation(
                            owner,
                            accountName,
                            sidechainPool.ProducersInPool.GetEnumerable().Where(p => p.ProducerInfo.ProducerType != ProducerTypeEnum.Validator).Select(p => p.ProducerInfo.AccountName).ToList(),
                            proposalName
                            );
                        _logger.LogDebug($"Added block byte and proposed history validation.");
                    }
                    catch (ApiErrorException apiException)
                    {
                        _logger.LogCritical($"Unable to add block byte or propose history validation with error: {apiException?.error?.name}");
                    }

                    await TryApproveAndExecuteHistoryValidation(accountName, owner, blockhash);
                    break;
                }
                else
                {
                    await _mainchainService.CancelTransaction(accountName, proposal.ProposalName);
                }

                await Task.Delay(100);
                historyTable = await _mainchainService.RetrieveHistoryValidationTable(owner);
            }

            await CheckSidechainValidationProposal(accountName, owner);
            TaskContainer = null;
        }

        public async Task TryApproveAndExecuteHistoryValidation(string accountName, string owner, string blockHashToCheck)
        {
            var historyTable = await _mainchainService.RetrieveHistoryValidationTable(owner);
            while (historyTable != null && historyTable.Key == accountName && historyTable.BlockHash == blockHashToCheck)
            {
                var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
                var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
                var proposalName = firstHalf + "hi" + secondHalf;

                var proposal = await _mainchainService.RetrieveProposal(historyTable.Key, proposalName);
                var approvals = await _mainchainService.RetrieveApprovals(historyTable.Key, proposalName);

                try
                {
                    if (proposal != null && approvals?.ProvidedApprovals?.Where(a => a.PermissionLevel.actor == accountName).FirstOrDefault() == null)
                    {
                        await _mainchainService.ApproveTransaction(historyTable.Key, proposal.ProposalName, accountName, proposal.TransactionHash);
                        _logger.LogDebug($"Approved history validation.");
                        await Task.Delay(500);
                    }
                    else if (approvals?.ProvidedApprovals?.Count >= approvals?.RequestedApprovals?.Count + 1)
                    {
                        await _mainchainService.ExecuteTransaction(historyTable.Key, proposal.ProposalName, accountName);
                        _logger.LogDebug($"Executed history validation.");
                        await Task.Delay(500);
                        break;
                    }
                }
                catch (ApiErrorException apiException)
                {
                    _logger.LogCritical($"Unable to approve or execute proposed history validation: {apiException?.error?.name}");
                }

                await Task.Delay(100);
                historyTable = await _mainchainService.RetrieveHistoryValidationTable(owner);
            }
        }

        public async Task CheckAndApproveHistoryValidation(string accountName, string owner)
        {
            var historyTable = await _mainchainService.RetrieveHistoryValidationTable(owner);
            if (historyTable != null)
            {
                var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
                var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
                var proposalName = firstHalf + "hi" + secondHalf;
                // logger.LogDebug($"Proposal name: {proposalName}.");

                var proposal = await _mainchainService.RetrieveProposal(historyTable.Key, proposalName);
                var approvals = await _mainchainService.RetrieveApprovals(historyTable.Key, proposalName);

                if (proposal != null && approvals?.ProvidedApprovals?.Where(a => a.PermissionLevel.actor == accountName).FirstOrDefault() == null)
                {
                    var validationAnswer = await GetValidationAnswer(historyTable.BlockHash, owner);
                    // logger.LogDebug($"Proposed answer/calculated answer {historyTable.BlockByteInHexadecimal}/{validationAnswer}");

                    if (validationAnswer == historyTable.BlockByteInHexadecimal)
                    {
                        try
                        {
                            await _mainchainService.ApproveTransaction(historyTable.Key, proposal.ProposalName, accountName, proposal.TransactionHash);
                            _logger.LogDebug($"Executed history validation.");
                        }
                        catch (ApiErrorException apiException)
                        {
                            _logger.LogCritical($"Unable to approve history validation transaction with error: {apiException?.error?.name}");
                        }
                    }
                }
            }
        }

        public async Task CheckSidechainValidationProposal(string accountName, string owner)
        {
            try
            {
                var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
                var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
                var proposalName = firstHalf + "hi" + secondHalf;
                // logger.LogDebug($"Proposal name: {proposalName}.");

                var proposal = await _mainchainService.RetrieveProposal(accountName, proposalName);
                if (proposal == null) return;

                await _mainchainService.CancelTransaction(accountName, proposal.ProposalName);
            }
            catch (ApiErrorException apiException)
            {
                _logger.LogCritical($"Unable to cancel existing history validation proposal with error: {apiException?.error?.name}");
            }
        }

        private async Task<string> GetValidationAnswer(string blockhash, string clientAccountName)
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

        //TODO: refactor, code repeated
        public async Task<ulong?> GetChosenBlockSequenceNumber(string blockhash, string clientAccountName)
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
            return chosenBlockSequenceNumber;
        }

    }
}