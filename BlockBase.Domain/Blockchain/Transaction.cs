using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using BlockBase.Domain.Database.Operations;
using BlockBase.Domain.Protos;
using Google.Protobuf;

namespace BlockBase.Domain.Blockchain
{
    public class Transaction : ICloneable
    {
        public byte[] TransactionHash { get; set; }
        public ulong SequenceNumber { get; set; }
        public string Signature { get; set; }
        public ulong Timestamp { get; set; }
        public string Json { get; set; }
        public string DatabaseName { get; set; }
        public bool IsReadQuery { get; set; }
        public byte[] BlockHash { get; set; }

        public Transaction() { }

        public Transaction(byte[] transactionHash, ulong sequenceNumber, string signature, bool isReadQuery, byte[] blockHash, string json, string databaseName, ulong? timestamp = null)
        {
            IsReadQuery = isReadQuery;
            TransactionHash = transactionHash;
            SequenceNumber = sequenceNumber;
            Signature = signature;
            Timestamp = timestamp ?? (ulong) ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            Json = json;
            BlockHash = blockHash;
            DatabaseName = databaseName;
        }

        public TransactionProto ConvertToProto()
        {
            var transactionProto = new TransactionProto()
            {
                TransactionHash = ByteString.CopyFrom(TransactionHash),
                SequenceNumber = SequenceNumber,
                Signature = Signature,
                Timestamp = Timestamp,
                Json = Json,
                BlockHash = ByteString.CopyFrom(BlockHash),
                DatabaseName = DatabaseName,
                IsReadQuery = IsReadQuery
            };

            return transactionProto;
        }

        public Transaction SetValuesFromProto(TransactionProto transactionProto)
        {
            TransactionHash = transactionProto.TransactionHash.ToByteArray();
            Signature = transactionProto.Signature;
            Timestamp = transactionProto.Timestamp;
            SequenceNumber = transactionProto.SequenceNumber;
            Json = transactionProto.Json;
            BlockHash = transactionProto.BlockHash.ToByteArray();
            DatabaseName = transactionProto.DatabaseName;
            IsReadQuery = transactionProto.IsReadQuery;

            return this;
        }

        public object Clone()
        {
            return new Transaction(TransactionHash, SequenceNumber, Signature, IsReadQuery, BlockHash, Json, DatabaseName, Timestamp);
        }
    }
}
