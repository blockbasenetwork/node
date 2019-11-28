﻿using BlockBase.Domain.Eos;
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
        public uint NumberOfTransactions { get; set; } 
        public string ProducerSignature { get; set; }
        public byte[] MerkleRoot { get; set; }

        public BlockHeader()
        {
        }

        public BlockHeader(byte[] blockHash, byte[] previousBlockHash, string producer, string producerSignature, byte[] merkleRoot,
            ulong sequenceNumber, ulong? timestamp = null, uint numberOfTransactions = 0)
        {
            BlockHash = blockHash;
            PreviousBlockHash = previousBlockHash;
            MerkleRoot = merkleRoot;
            SequenceNumber = sequenceNumber;
            ProducerSignature = producerSignature;
            Producer = producer;
            Timestamp = timestamp ?? (ulong) ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            NumberOfTransactions = numberOfTransactions;
        }

        public Dictionary<string, object> ConvertToEosObject()
        {
            return new Dictionary<string, object>()
            {
                { EosParameterNames.PRODUCER, Producer },
                { EosParameterNames.BLOCK_HASH, HashHelper.ByteArrayToFormattedHexaString(BlockHash) },
                { EosParameterNames.PREVIOUS_BLOCK_HASH, HashHelper.ByteArrayToFormattedHexaString(PreviousBlockHash) },
                { EosParameterNames.SEQUENCE_NUMBER, SequenceNumber},
                { EosParameterNames.TIMESTAMP, Timestamp },
                { EosParameterNames.NUMBER_OF_TRANSACTIONS, NumberOfTransactions},
                { EosParameterNames.PRODUCER_SIGNATURE, ProducerSignature},
                { EosParameterNames.MERKLE_TREE_ROOT_HASH, HashHelper.ByteArrayToFormattedHexaString(MerkleRoot) },
                { EosParameterNames.IS_VERIFIED, false },
                { EosParameterNames.IS_LAST_BLOCK, false }
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
                NumberOfTransactions = NumberOfTransactions,
                ProducerSignature = ProducerSignature,
                MerkleRoot = ByteString.CopyFrom(MerkleRoot)
            };

            return blockHeaderProto;
        }

        public BlockHeader SetValuesFromDictionary(Dictionary<string, object> dic)
        {
            BlockHash = HashHelper.FormattedHexaStringToByteArray((string) dic[EosParameterNames.BLOCK_HASH]);
            Producer = (string) dic[EosParameterNames.PRODUCER];
            PreviousBlockHash = HashHelper.FormattedHexaStringToByteArray((string)dic[EosParameterNames.PREVIOUS_BLOCK_HASH]);
            SequenceNumber = (ulong) dic[EosParameterNames.SEQUENCE_NUMBER];
            Timestamp = (ulong) dic[EosParameterNames.TIMESTAMP];
            NumberOfTransactions = (uint) dic[EosParameterNames.NUMBER_OF_TRANSACTIONS];
            ProducerSignature = (string) dic[EosParameterNames.PRODUCER_SIGNATURE];
            MerkleRoot = HashHelper.FormattedHexaStringToByteArray((string) dic[EosParameterNames.MERKLE_TREE_ROOT_HASH]);

            return this;
        }

        public BlockHeader SetValuesFromProto(BlockHeaderProto blockHeaderProto)
        {
            BlockHash = blockHeaderProto.BlockHash.ToByteArray();
            Producer = blockHeaderProto.Producer;
            PreviousBlockHash = blockHeaderProto.PreviousBlockHash.ToByteArray();
            SequenceNumber = blockHeaderProto.SequenceNumber;
            Timestamp =  blockHeaderProto.Timestamp;
            NumberOfTransactions = blockHeaderProto.NumberOfTransactions;
            ProducerSignature = blockHeaderProto.ProducerSignature;
            MerkleRoot = blockHeaderProto.MerkleRoot.ToByteArray();

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
            if(NumberOfTransactions != item.NumberOfTransactions) return false;
            if(!ProducerSignature.SequenceEqual(item.ProducerSignature)) return false;
            if(Producer != item.Producer) return false;
            if(Timestamp != item.Timestamp) return false;

            return true;
        }

        public object Clone()
        {
           return new BlockHeader(BlockHash, PreviousBlockHash, Producer, ProducerSignature, MerkleRoot, SequenceNumber, Timestamp, NumberOfTransactions);
        }
    }
}
