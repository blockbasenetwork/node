using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Protos;
using BlockBase.Network.Sidechain;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.Sidechain.Helpers
{
    public static class ValidationHelper
    {

         public static bool ValidateBlockAndBlockheader(Block blockReceived, SidechainPool sidechainPool, BlockHeader blockheaderFromSmartContract, ILogger logger, out byte[] trueBlockHash)
        {

            if (!IsBlockHashValid(blockReceived.BlockHeader, out trueBlockHash))
            {
                logger.LogDebug("Blockhash doesn't match block content.");
                return false;
            }

            if (!blockheaderFromSmartContract.Equals(blockReceived.BlockHeader))
            {
                logger.LogDebug("Block received is not the same as the block submitted to smart contract.");
                logger.LogDebug($"SC: {JsonConvert.SerializeObject(blockheaderFromSmartContract)} \nR: {JsonConvert.SerializeObject(blockReceived.BlockHeader)}");
                return false;
            }

            var producerPublicKey = sidechainPool.ProducersInPool.GetEnumerable().Where(m => m.ProducerInfo.AccountName == blockReceived.BlockHeader.Producer).Select(n => n.ProducerInfo.PublicKey).SingleOrDefault();

            if (!SignatureHelper.VerifySignature(producerPublicKey, blockReceived.BlockHeader.ProducerSignature, blockReceived.BlockHeader.BlockHash))
            {
                logger.LogDebug("Invalid signature.");
                return false;
            }
            return true;
        }

        internal static bool IsBlockHashValid(BlockHeader blockHeader, out byte[] blockHash)
        {
            var auxBlockHeader = (BlockHeader)blockHeader.Clone();

            auxBlockHeader.BlockHash = new byte[0];
            auxBlockHeader.ProducerSignature = "";
            auxBlockHeader.BlockSizeInBytes = 0;

            var serializedBlockHeader = JsonConvert.SerializeObject(auxBlockHeader);
            blockHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedBlockHeader));

            if (!blockHash.SequenceEqual(blockHeader.BlockHash))
            {
                return false;
            }

            return true;
        }

        internal static bool IsTransactionHashValid(Transaction transaction, out byte[] transactionHash)
        {
            var auxTransaction = (Transaction)transaction.Clone();

            auxTransaction.Signature = "";
            auxTransaction.TransactionHash = new byte[0];

            var serializedTransaction = JsonConvert.SerializeObject(auxTransaction);
            transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

            if (!transactionHash.SequenceEqual(transaction.TransactionHash))
            {
                return false;
            }

            return true;
        }
       
    }
}