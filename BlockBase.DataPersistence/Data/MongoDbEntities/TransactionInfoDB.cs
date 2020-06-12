using MongoDB.Bson.Serialization.Attributes;

namespace BlockBase.DataPersistence.Data.MongoDbEntities
{
    public class TransactionInfoDB
    {
        [BsonId()]
        public string BlockHash { get; set; }

        [BsonElement("LastIncludedSequenceNumber")]
        [BsonRequired()]
        public ulong LastIncludedSequenceNumber { get; set; }
    }
}