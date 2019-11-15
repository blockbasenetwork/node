
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
using BlockBase.Utils;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace BlockBase.DataPersistence
{
    public class TestSidechainManager
    {
        IMongoDatabase Database { get; set; }
        IMongoClient Client { get; set; }
        IClientSession Session { get; set; }
        public void Execute()
        {
            Client = new MongoClient(MongoDbConstants.CONNECTION_STRING);
            Database = Client.GetDatabase(MongoDbConstants.RECOVER_DATABASE_NAME);
            Session = Client.StartSession();

            var collectionSidechains = Database.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME);
            var sidechain = new SidechainDB { Id = "Example" };
            collectionSidechains.InsertOneAsync(sidechain);

            var collectionBlocks = Database.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
            var block = new BlockheaderDB
            {
                Producer = "Fernando",
                SequenceNumber = 1,
                Timestamp = (uint) DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            collectionBlocks.InsertOneAsync(block);
        }
    }
}
