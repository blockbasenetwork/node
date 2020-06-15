using BlockBase.Domain.Eos;
using BlockBase.Domain.Protos;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using BlockBase.Utils.Crypto;
using System.Linq;

namespace BlockBase.Domain.Blockchain
{
    [Serializable]
    public class BlockHeader : ICloneable
    {
        public string Producer { get; set; }
        public byte[] BlockHash { get; set; }
        public byte[] PreviousBlockHash { get; set; }
        public ulong SequenceNumber { get; set; }
        public ulong Timestamp { get; set; }
        public ulong BlockSizeInBytes { get; set; }
        public uint TransactionCount { get; set; } 
        public string ProducerSignature { get; set; }
        public byte[] MerkleRoot { get; set; }
        public ulong LastTransactionSequenceNumber { get; set; }

        public BlockHeader()
        {
        }

        public BlockHeader(byte[] blockHash,ulong blockSizeInBytes, byte[] previousBlockHash, string producer, string producerSignature, byte[] merkleRoot,
            ulong sequenceNumber, uint transactionCount, ulong lastTransactionSequenceNumber, ulong? timestamp = null)
        {
            BlockHash = blockHash;
            BlockSizeInBytes = blockSizeInBytes;
            PreviousBlockHash = previousBlockHash;
            MerkleRoot = merkleRoot;
            SequenceNumber = sequenceNumber;
            ProducerSignature = producerSignature;
            Producer = producer;
            Timestamp = timestamp ?? (ulong) ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            TransactionCount = transactionCount;
            LastTransactionSequenceNumber = lastTransactionSequenceNumber;
        }

        public Dictionary<string, object> ConvertToEosObject()
        {
            
            return new Dictionary<string, object>()
            {
                { EosAtributeNames.PRODUCER, Producer },
                { EosAtributeNames.BLOCK_HASH, HashHelper.ByteArrayToFormattedHexaString(BlockHash) },
                { EosAtributeNames.PREVIOUS_BLOCK_HASH, HashHelper.ByteArrayToFormattedHexaString(PreviousBlockHash) },
                { EosAtributeNames.SEQUENCE_NUMBER, SequenceNumber},
                { EosAtributeNames.TIMESTAMP, Timestamp },
                { EosAtributeNames.BLOCK_SIZE_IN_BYTES, BlockSizeInBytes },
                { EosAtributeNames.TRANSACTIONS_COUNT, TransactionCount},
                { EosAtributeNames.PRODUCER_SIGNATURE, ProducerSignature},
                { EosAtributeNames.MERKLETREE_ROOT_HASH, HashHelper.ByteArrayToFormattedHexaString(MerkleRoot) },
                { EosAtributeNames.IS_VERIFIED, false },
                { EosAtributeNames.IS_LATEST_BLOCK, false },
                { EosAtributeNames.LAST_TRANSACTION_SEQUENCE_NUMBER, LastTransactionSequenceNumber }
            };
        }

        public BlockHeaderProto ConvertToProto()
        {
            var blockHeaderProto = new BlockHeaderProto()
            {
                Producer = Producer,
                BlockHash = ByteString.CopyFrom(BlockHash),
                PreviousBlockHash = ByteString.CopyFrom(PreviousBlockHash),
                SequenceNumber = SequenceNumber,
                Timestamp = Timestamp,
                TransactionCount = TransactionCount,
                ProducerSignature = ProducerSignature,
                MerkleRoot = ByteString.CopyFrom(MerkleRoot),
                BlockSizeInBytes = BlockSizeInBytes,
                LastTransactionSequenceNumber = LastTransactionSequenceNumber
            };

            return blockHeaderProto;
        }

        public BlockHeader SetValuesFromDictionary(Dictionary<string, object> dic)
        {
            BlockHash = HashHelper.FormattedHexaStringToByteArray((string) dic[EosAtributeNames.BLOCK_HASH]);
            Producer = (string) dic[EosAtributeNames.PRODUCER];
            PreviousBlockHash = HashHelper.FormattedHexaStringToByteArray((string)dic[EosAtributeNames.PREVIOUS_BLOCK_HASH]);
            SequenceNumber = (ulong) dic[EosAtributeNames.SEQUENCE_NUMBER];
            Timestamp = (ulong) dic[EosAtributeNames.TIMESTAMP];
            BlockSizeInBytes = (ulong) dic[EosAtributeNames.BLOCK_SIZE_IN_BYTES];
            TransactionCount = (uint) dic[EosAtributeNames.TRANSACTIONS_COUNT];
            ProducerSignature = (string) dic[EosAtributeNames.PRODUCER_SIGNATURE];
            MerkleRoot = HashHelper.FormattedHexaStringToByteArray((string) dic[EosAtributeNames.MERKLETREE_ROOT_HASH]);
            LastTransactionSequenceNumber = (ulong) dic[EosAtributeNames.LAST_TRANSACTION_SEQUENCE_NUMBER];

            return this;
        }

        public BlockHeader SetValuesFromProto(BlockHeaderProto blockHeaderProto)
        {
            BlockHash = blockHeaderProto.BlockHash.ToByteArray();
            Producer = blockHeaderProto.Producer;
            PreviousBlockHash = blockHeaderProto.PreviousBlockHash.ToByteArray();
            SequenceNumber = blockHeaderProto.SequenceNumber;
            Timestamp =  blockHeaderProto.Timestamp;
            BlockSizeInBytes = blockHeaderProto.BlockSizeInBytes;
            TransactionCount = blockHeaderProto.TransactionCount;
            ProducerSignature = blockHeaderProto.ProducerSignature;
            MerkleRoot = blockHeaderProto.MerkleRoot.ToByteArray();
            LastTransactionSequenceNumber = blockHeaderProto.LastTransactionSequenceNumber;

            return this;
        }

        public override bool Equals(object obj)
        {
            var item = obj as BlockHeader;

            if(item == null) return false;
            if(!BlockHash.SequenceEqual(item.BlockHash)) return false;
            if(!PreviousBlockHash.SequenceEqual(item.PreviousBlockHash)) return false;
            if(!MerkleRoot.SequenceEqual(item.MerkleRoot)) return false;
            if(SequenceNumber != item.SequenceNumber) return false;
            if(TransactionCount != item.TransactionCount) return false;
            if(!ProducerSignature.SequenceEqual(item.ProducerSignature)) return false;
            if(Producer != item.Producer) return false;
            if(Timestamp != item.Timestamp) return false;
            if(BlockSizeInBytes != item.BlockSizeInBytes) return false;
            if(LastTransactionSequenceNumber != item.LastTransactionSequenceNumber) return false;

            return true;
        }

        public override int GetHashCode() 
        {
            return base.GetHashCode();
        }

        public object Clone()
        {
           return new BlockHeader(BlockHash, BlockSizeInBytes, PreviousBlockHash, Producer, ProducerSignature, MerkleRoot, SequenceNumber, TransactionCount, LastTransactionSequenceNumber, Timestamp);
        }
    }
}
