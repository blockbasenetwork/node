using BlockBase.Domain.Blockchain;
using BlockBase.Utils.Crypto;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataPersistence.ProducerData.MongoDbEntities
{
    public class BlockheaderDB
    {
        [BsonId()]
        public string BlockHash { get; set; }

        [BsonElement("PreviousBlockhash")]
        [BsonRequired()]
        public string PreviousBlockhash;

        [BsonElement("Producer")]
        [BsonRequired()]
        public string Producer { get; set; }

        [BsonElement("ProducerSignature")]
        [BsonRequired()]
        public string ProducerSignature { get; set; }

        [BsonElement("Timestamp")]
        [BsonRequired()]
        public ulong Timestamp { get; set; }

        [BsonElement("SequenceNumber")]
        [BsonRequired()]
        public ulong SequenceNumber { get; set; }

        [BsonElement("MerkleRoot")]
        [BsonRequired()]
        public byte[] MerkleRoot { get; set; }

        [BsonElement("TransactionCount")]
        [BsonRequired()]
        public uint TransactionCount { get; set; }

        [BsonElement("Confirmed")]
        [BsonRequired()]
        public bool Confirmed { get; set; }

        [BsonElement("BlockSizeInBytes")]
        [BsonRequired()]
        public ulong BlockSizeInBytes { get; set; }

        public BlockheaderDB BlockheaderDBFromBlockHeader(BlockHeader blockHeader)
        {

            BlockHash = HashHelper.ByteArrayToFormattedHexaString(blockHeader.BlockHash);
            PreviousBlockhash = HashHelper.ByteArrayToFormattedHexaString(blockHeader.PreviousBlockHash);
            SequenceNumber = blockHeader.SequenceNumber;
            Timestamp = blockHeader.Timestamp;
            Producer = blockHeader.Producer;
            MerkleRoot = blockHeader.MerkleRoot;
            ProducerSignature = blockHeader.ProducerSignature;
            Confirmed = false;
            TransactionCount = blockHeader.TransactionCount;
            BlockSizeInBytes = blockHeader.BlockSizeInBytes;
            return this;
        }

        public BlockHeader BlockHeaderFromBlockHeaderDB()
        {
            return new BlockHeader(HashHelper.FormattedHexaStringToByteArray(BlockHash), BlockSizeInBytes,
            HashHelper.FormattedHexaStringToByteArray(PreviousBlockhash), Producer, ProducerSignature, MerkleRoot, SequenceNumber, TransactionCount, Timestamp); 
        }
    }
}
