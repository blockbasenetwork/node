using BlockBase.Domain.Blockchain;
using BlockBase.Utils.Crypto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class BlockheaderTable
    {
        [JsonProperty("blockhash")]
        public string BlockHash { get; set; }

        [JsonProperty("previousblockhash")]
        public string PreviousBlockHash { get; set; }

        [JsonProperty("sequencenumber")]
        public uint SequenceNumber { get; set; }

        [JsonProperty("timestamp")]
        public uint Timestamp { get; set; }

        [JsonProperty("transactionnumber")]
        public uint TransactionCount { get; set; }

        [JsonProperty("producer")]
        public string Producer { get; set; }

        [JsonProperty("producersignature")]
        public string ProducerSignature { get; set; }

        [JsonProperty("merkletreeroothash")]
        public string MerkleTreeRootHash { get; set; }

        [JsonProperty("isverified")]
        public bool IsVerified { get; set; }
        
        [JsonProperty("islasblock")]
        public bool IsLastBlock { get; set; }


        public BlockHeader ConvertToBlockHeader()
        {
            
            return new BlockHeader(HashHelper.FormattedHexaStringToByteArray(BlockHash),
                    HashHelper.FormattedHexaStringToByteArray(PreviousBlockHash), 
                    Producer, ProducerSignature, 
                    HashHelper.FormattedHexaStringToByteArray(MerkleTreeRootHash), 
                    SequenceNumber, Timestamp);
        }
    }
}
