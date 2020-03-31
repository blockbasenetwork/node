using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Sidechain
{
    public static class HistoryValidationHelper
    {
        private static Random _rnd = new Random();

        public static async Task SendRequestHistoryValidation(IMainchainService mainChainService, string clientAccountName, ILogger logger, List<ProducerInTable> producers)
        {
            var sidechainConfig = await mainChainService.RetrieveContractInformation(clientAccountName);
            var lastValidBlockheaderTable = await mainChainService.GetLastValidSubmittedBlockheader(clientAccountName, (int)sidechainConfig.BlocksBetweenSettlement);
            if (lastValidBlockheaderTable != null)
            {
                var validProducers = producers.Where(p => p.Warning != EosTableValues.WARNING_PUNISH).ToList();
                var lastValidBlockheader = lastValidBlockheaderTable.ConvertToBlockHeader();
                int r = _rnd.Next(validProducers.Count);
                var chosenProducerAccountName = validProducers[r].Key;
                try
                {
                    await mainChainService.RequestHistoryValidation(clientAccountName, chosenProducerAccountName, HashHelper.ByteArrayToFormattedHexaString(lastValidBlockheader.BlockHash));
                    logger.LogDebug("Updated history validation table.");
                }
                catch (ApiErrorException apiException)
                {
                    logger.LogWarning($"Unable to request history validation with error: {apiException?.error?.name}");
                }
            }
        }

        public static async Task ProposeHistoryValidationAndTryToExecute(IMainchainService mainChainService, IMongoDbProducerService mongoDbProducerService, string accountName, string blockhash, SidechainPool sidechainPool, ILogger logger)
        {
            var owner = sidechainPool.ClientAccountName;
            var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
            var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
            var proposalName = firstHalf + "hi" + secondHalf;

            var proposal = await mainChainService.RetrieveProposal(accountName, proposalName);

            if (proposal == null)
            {
                var validationAnswer = await GetValidationAnswer(mongoDbProducerService, blockhash, sidechainPool.ClientAccountName, logger);
                // logger.LogDebug($"Owner: {owner}, Producer {accountName}, ValidationAnswer: {validationAnswer}");
                try
                {
                    await mainChainService.AddBlockByte(owner, accountName, validationAnswer);
                    await mainChainService.ProposeHistoryValidation(
                        sidechainPool.ClientAccountName,
                        accountName,
                        sidechainPool.ProducersInPool.GetEnumerable().Select(p => p.ProducerInfo.AccountName).ToList(),
                        proposalName
                        );
                    logger.LogDebug($"Added block byte and proposed history validation.");
                }
                catch (ApiErrorException apiException)
                {
                    logger.LogCritical($"Unable to add block byte or propose history validation with error: {apiException?.error?.name}");
                }

            }
            await CheckAndApproveHistoryValidation(mainChainService, mongoDbProducerService, accountName, sidechainPool.ClientAccountName, logger);
        }

        public static async Task CheckAndApproveHistoryValidation(IMainchainService mainChainService, IMongoDbProducerService mongoDbProducerService, string accountName, string owner, ILogger logger)
        {
            var historyTable = await mainChainService.RetrieveHistoryValidationTable(owner);
            if (historyTable != null)
            {
                var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
                var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
                var proposalName = firstHalf + "hi" + secondHalf;
                // logger.LogDebug($"Proposal name: {proposalName}.");

                var proposal = await mainChainService.RetrieveProposal(historyTable.Key, proposalName);
                var approvals = await mainChainService.RetrieveApprovals(historyTable.Key, proposalName);

                if (proposal != null && approvals?.ProvidedApprovals?.Where(a => a.PermissionLevel.actor == accountName).FirstOrDefault() == null)
                {
                    var validationAnswer = await GetValidationAnswer(mongoDbProducerService, historyTable.BlockHash, owner, logger);
                    // logger.LogDebug($"Proposed answer/calculated answer {historyTable.BlockByteInHexadecimal}/{validationAnswer}");

                    if (validationAnswer == historyTable.BlockByteInHexadecimal)
                    {
                        try
                        {
                            await mainChainService.ApproveTransaction(historyTable.Key, proposal.ProposalName, accountName, proposal.TransactionHash);
                        }
                        catch (ApiErrorException apiException)
                        {
                            logger.LogCritical($"Unable to approve history validation transaction with error: {apiException?.error?.name}");
                        }
                    }
                }
                else if (approvals?.ProvidedApprovals?.Count >= approvals?.RequestedApprovals?.Count + 1)
                {
                    try
                    {
                        await mainChainService.ExecuteTransaction(historyTable.Key, proposal.ProposalName, accountName);
                        logger.LogDebug($"Executed history validation.");
                    }
                    catch (ApiErrorException apiException)
                    {
                        logger.LogCritical($"Unable to execute history validation transaction with error: {apiException?.error?.name}");
                    }
                }
            }
        }

        public static async Task CheckSidechainValidationProposal(IMainchainService mainChainService, string accountName, string owner, ILogger logger)
        {
            try
            {
                var firstHalf = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length);
                var secondHalf = owner.Substring(owner.Length - 5 > 0 ? owner.Length - 5 : 0, owner.Length >= 5 ? 5 : owner.Length);
                var proposalName = firstHalf + "hi" + secondHalf;
                // logger.LogDebug($"Proposal name: {proposalName}.");

                var proposal = await mainChainService.RetrieveProposal(accountName, proposalName);
                if (proposal == null) return;

                await mainChainService.CancelTransaction(accountName, proposal.ProposalName);
            }
            catch (ApiErrorException apiException)
            {
                logger.LogCritical($"Unable to cancel existing history validation proposal with error: {apiException?.error?.name}");
            }
        }

        private static async Task<string> GetValidationAnswer(IMongoDbProducerService mongoDbProducerService, string blockhash, string clientAccountName, ILogger logger)
        {
            var block = await mongoDbProducerService.GetSidechainBlockAsync(clientAccountName, blockhash);

            if (block == null)
            {
                logger.LogWarning("Producer does not have most current block for history validation.");
                return null;
            }

            var blockHashNumber = BitConverter.ToUInt64(HashHelper.FormattedHexaStringToByteArray(blockhash));
            // logger.LogWarning("Blockhash converted to number: " + blockHashNumber);

            var chosenBlockSequenceNumber = (blockHashNumber % block.BlockHeader.SequenceNumber) + 1;
            // logger.LogWarning($"Current block sequence number {block.BlockHeader.SequenceNumber}, chosen block sequence number {chosenBlockSequenceNumber}");

            var chosenBlock = (await mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(clientAccountName, chosenBlockSequenceNumber, chosenBlockSequenceNumber)).SingleOrDefault();
            if (chosenBlock == null)
            {
                logger.LogWarning("Producer does not have randomly chosen block for history validation.");
                return null;
            }

            var blockBytes = chosenBlock.ConvertToProto().ToByteArray();
            var byteIndex = (int)(blockHashNumber % (ulong)blockBytes.Count());
            // logger.LogWarning($"Number of block bytes {blockBytes.Count()}, chosen byte{byteIndex}");

            return HashHelper.ByteArrayToFormattedHexaString(new byte[] { blockBytes[byteIndex] });
        }

        //TODO: refactor, code repeated
        public static async Task<ulong?> GetChosenBlockSequenceNumber(IMongoDbProducerService mongoDbProducerService, string blockhash, string clientAccountName, ILogger logger)
        {
            var block = await mongoDbProducerService.GetSidechainBlockAsync(clientAccountName, blockhash);

            if (block == null)
            {
                logger.LogWarning("Producer does not have most current block for history validation.");
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