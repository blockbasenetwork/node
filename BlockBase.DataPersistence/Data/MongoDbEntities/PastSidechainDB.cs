using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataPersistence.Data.MongoDbEntities
{
    public class PastSidechainDB
    {
        [BsonId()]
        public ObjectId _id { get; set; }

        [BsonElement("Sidechain")]
        [BsonRequired()]
        public string Sidechain { get; set; }

        [BsonElement("Timestamp")]
        [BsonRequired()]
        public ulong Timestamp { get; set; }

        [BsonElement("DateLeftTimestamp")]
        public ulong DateLeftTimestamp { get; set; }

        [BsonElement("AlreadyLeft")]
        public bool AlreadyLeft { get; set; }

        [BsonElement("ReasonLeft")]
        public string ReasonLeft { get; set; }

        public PastSidechainDB()
        {
            _id = ObjectId.GenerateNewId();
        }
    }
}
