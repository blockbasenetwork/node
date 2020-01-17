﻿using BlockBase.Domain.Blockchain;
using BlockBase.Utils.Crypto;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlockBase.DataPersistence.ProducerData.MongoDbEntities
{
    public class TransactionDB
    {
        [BsonId()]
        public string TransactionHash { get; set; }

        [BsonElement("SequenceNumber")]
        [BsonRequired()]
        public ulong SequenceNumber { get; set; }

        [BsonElement("TransactionJson")]
        [BsonRequired()]
        public string TransactionJson { get; set; }

        [BsonElement("Signature")]
        [BsonRequired()]
        public string Signature { get; set; }

        //ITS MORE EFFICIENT BECAUSE 1-TO-SQUILLIONS
        [BsonElement("Blockhash")]
        public string BlockHash { get; set; }

        [BsonElement("IsReadQuery")]
        [BsonRequired()]
        public bool IsReadQuery { get; set; }

        [BsonElement("Timestamp")]
        [BsonRequired()]
        public ulong Timestamp { get; set; }

        [BsonElement("DatabaseName")]
        [BsonRequired()]
        public string DatabaseName { get; set; }

        public Transaction TransactionFromTransactionDB()
        {
            var transaction = new Transaction();
            transaction.TransactionHash = HashHelper.FormattedHexaStringToByteArray(TransactionHash);
            transaction.Signature = Signature;
            transaction.Timestamp = Timestamp;
            transaction.SequenceNumber = SequenceNumber;
            transaction.BlockHash = HashHelper.FormattedHexaStringToByteArray(BlockHash);
            transaction.Json = TransactionJson;
            transaction.DatabaseName = DatabaseName;
            transaction.IsReadQuery = IsReadQuery;
            return transaction;
        }

        public TransactionDB TransactionDBFromTransaction(Transaction transaction)
        {
            TransactionHash = HashHelper.ByteArrayToFormattedHexaString(transaction.TransactionHash);
            TransactionJson = transaction.Json;
            Signature = transaction.Signature;
            BlockHash = HashHelper.ByteArrayToFormattedHexaString(transaction.BlockHash);
            Timestamp = transaction.Timestamp;
            SequenceNumber = transaction.SequenceNumber;
            DatabaseName = transaction.DatabaseName;
            IsReadQuery = transaction.IsReadQuery;
            return this;
        }
    }
}
