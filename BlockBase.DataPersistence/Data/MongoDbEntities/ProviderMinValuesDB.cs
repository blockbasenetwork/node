using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataPersistence.Data.MongoDbEntities
{
    public class ProviderMinValuesDB
    {
        [BsonId()]
        public ObjectId _id { get; set; }

        [BsonElement("Timestamp")]
        public ulong Timestamp { get; set; }

        [BsonElement("FullNodeMinBBTPerEmptyBlock")]
        [BsonRequired()]
        public decimal FullNodeMinBBTPerEmptyBlock { get; set; }

        [BsonElement("FullNodeMinBBTPerMBRatio")]
        [BsonRequired()]
        public decimal FullNodeMinBBTPerMBRatio { get; set; }

        [BsonElement("HistoryNodeMinBBTPerEmptyBlock")]
        [BsonRequired()]
        public decimal HistoryNodeMinBBTPerEmptyBlock { get; set; }

        [BsonElement("HistoryNodeMinBBTPerMBRatio")]
        [BsonRequired()]
        public decimal HistoryNodeMinBBTPerMBRatio { get; set; }

        [BsonElement("ValidatorNodeMinBBTPerEmptyBlock")]
        [BsonRequired()]
        public decimal ValidatorNodeMinBBTPerEmptyBlock { get; set; }

        [BsonElement("ValidatorNodeMinBBTPerMBRatio")]
        [BsonRequired()]
        public decimal ValidatorNodeMinBBTPerMBRatio { get; set; }

        public ProviderMinValuesDB()
        {
            _id = ObjectId.GenerateNewId();
        }
    }
}
