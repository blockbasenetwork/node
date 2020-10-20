using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataPersistence.Data.MongoDbEntities
{
    public class BBTValueDB
    {
        [BsonId()]
        public ObjectId _id { get; set; }

        [BsonElement("Timestamp")]
        public ulong Timestamp { get; set; }

        [BsonElement("ValueInUSD")]
        public double ValueInUSD { get; set; }

        public BBTValueDB()
        {
            _id = ObjectId.GenerateNewId();
        }
    }
}
