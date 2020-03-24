using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Utils.Crypto;
using Google.Protobuf;

namespace BlockBase.Runtime.Sidechain
{
    public static class HistoryValidationHelper
    {
        private static Random _rnd = new Random();

        public static async Task SendRequestHistoryValidation(IMainchainService mainChainService, string clientAccountName)
        {
            var sidechainConfig = await mainChainService.RetrieveContractInformation(clientAccountName);
            var lastValidBlockheader = (await mainChainService.GetLastValidSubmittedBlockheader(clientAccountName, (int)sidechainConfig.BlocksBetweenSettlement)).ConvertToBlockHeader();
            var producers = await mainChainService.RetrieveProducersFromTable(clientAccountName);
            int r = _rnd.Next(producers.Count);
            var chosenProducerAccountName = producers[r].Key;

            await mainChainService.RequestHistoryValidation(clientAccountName, chosenProducerAccountName, HashHelper.ByteArrayToFormattedHexaString(lastValidBlockheader.BlockHash));
        }

        public static async Task ProposeHistoryValidation(IMainchainService mainChainService, IMongoDbProducerService mongoDbProducerService, string producerName, string blockhash, SidechainPool sidechainPool)
        {
            var validationAnswer = await GetValidationAnswer(mongoDbProducerService, producerName, blockhash, sidechainPool.ClientAccountName);

            await mainChainService.AddBlockByte(sidechainPool.ClientAccountName, producerName, validationAnswer);
            var owner = sidechainPool.ClientAccountName;
            var proposalName = owner.Substring(0, owner.Length >= 5 ? 5 : owner.Length - 1) + "hi" + owner.Substring(owner.Length - 5 >= 0 ? owner.Length - 5 : 0, owner.Length - 1);

            await mainChainService.ProposeHistoryValidation(
                sidechainPool.ClientAccountName,
                producerName,
                sidechainPool.ProducersInPool.GetEnumerable().Select(p => p.ProducerInfo.AccountName).ToList(),
                proposalName
                );


            var proposal = await mainChainService.RetrieveProposal(producerName, proposalName);
            var approvals = await mainChainService.RetrieveApprovals(producerName, proposalName); 

            if (proposal != null && approvals?.ProvidedApprovals?.Where(a => a.PermissionLevel.actor == producerName).FirstOrDefault() == null)
            {
                await mainChainService.ApproveTransaction(producerName, proposal.ProposalName, producerName, proposal.TransactionHash);;
            }
            else if (approvals?.ProvidedApprovals?.Count >= approvals?.RequestedApprovals?.Count + 1)
            {
                await mainChainService.ExecuteTransaction(producerName, proposal.ProposalName, producerName);
                return;
            }
        }

        private static async Task<string> GetValidationAnswer(IMongoDbProducerService mongoDbProducerService, string producerName, string blockhash, string clientAccountName)
        {
            var block = await mongoDbProducerService.GetSidechainBlockAsync(clientAccountName, blockhash);
            var blockHashNumber = BitConverter.ToInt32(HashHelper.FormattedHexaStringToByteArray(blockhash));
            var chosenBlockSequenceNumber = ((ulong)blockHashNumber % block.BlockHeader.SequenceNumber) + 1;

            var chosenBlock = (await mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(clientAccountName, chosenBlockSequenceNumber, chosenBlockSequenceNumber)).SingleOrDefault();
            var blockBytes = chosenBlock.ConvertToProto().ToByteArray();
            var byteIndex = blockHashNumber % blockBytes.Count();

            return HashHelper.ByteArrayToFormattedHexaString(new byte[] { blockBytes[byteIndex] });
        }
    }
}