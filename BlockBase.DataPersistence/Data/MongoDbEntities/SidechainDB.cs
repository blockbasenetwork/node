using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataPersistence.Data.MongoDbEntities
{
    public class SidechainDB
    {
        [BsonId()]
        public string Id { get; set; }

        [BsonElement("Timestamp")]
        // [BsonRequired()]
        public ulong Timestamp { get; set; }

        [BsonElement("ProducerType")]
        public int ProducerType { get; set; }

        [BsonElement("IsAutomatic")]
        // [BsonRequired()]
        public bool IsAutomatic { get; set; }
    }
}
