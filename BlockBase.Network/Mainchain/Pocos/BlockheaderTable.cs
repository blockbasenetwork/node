using BlockBase.Domain.Blockchain;
using BlockBase.Utils.Crypto;
using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class BlockheaderTable
    {
        [JsonProperty(EosAtributeNames.BLOCK_HASH)]
        public string BlockHash { get; set; }

        [JsonProperty(EosAtributeNames.PREVIOUS_BLOCK_HASH)]
        public string PreviousBlockHash { get; set; }

        [JsonProperty(EosAtributeNames.SEQUENCE_NUMBER)]
        public uint SequenceNumber { get; set; }

        [JsonProperty(EosAtributeNames.TIMESTAMP)]
        public uint Timestamp { get; set; }

        [JsonProperty(EosAtributeNames.TRANSACTIONS_COUNT)]
        public uint TransactionCount { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER)]
        public string Producer { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER_SIGNATURE)]
        public string ProducerSignature { get; set; }

        [JsonProperty(EosAtributeNames.MERKLETREE_ROOT_HASH)]
        public string MerkleTreeRootHash { get; set; }

        [JsonProperty(EosAtributeNames.IS_VERIFIED)]
        public bool IsVerified { get; set; }
        
        [JsonProperty(EosAtributeNames.IS_LATEST_BLOCK)]
        public bool IsLastBlock { get; set; }


        public BlockHeader ConvertToBlockHeader()
        {
            return new BlockHeader(HashHelper.FormattedHexaStringToByteArray(BlockHash),
                    HashHelper.FormattedHexaStringToByteArray(PreviousBlockHash), 
                    Producer, ProducerSignature, 
                    HashHelper.FormattedHexaStringToByteArray(MerkleTreeRootHash), 
                    SequenceNumber, TransactionCount, Timestamp);
        }
    }
}
