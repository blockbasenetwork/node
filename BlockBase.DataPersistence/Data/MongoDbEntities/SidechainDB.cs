﻿using MongoDB.Bson;
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
    }
}