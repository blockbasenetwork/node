using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MongoDB.Driver.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.Domain.Blockchain;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Options;
using BlockBase.Domain.Configurations;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;
using BlockBase.Domain.Enums;

namespace BlockBase.DataPersistence.Data
{
    public class MongoDbProducerService : AbstractMongoDbService, IMongoDbProducerService
    {
        public MongoDbProducerService(IOptions<NodeConfigurations> nodeConfigurations, ILogger<MongoDbProducerService> logger) : base(nodeConfigurations, logger)
        {
        }


        public async Task AddBlockToSidechainDatabaseAsync(Block block, string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var blockHeaderDB = new BlockheaderDB().BlockheaderDBFromBlockHeader(block.BlockHeader);
                await blockheaderCollection.InsertOneAsync(blockHeaderDB);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                foreach (var transaction in block.Transactions)
                {
                    var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
                    transactionDB.BlockHash = HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash);
                    await transactionCollection.ReplaceOneAsync(t => t.TransactionHash == transactionDB.TransactionHash, transactionDB, new UpdateOptions { IsUpsert = true });
                }
                await session.CommitTransactionAsync();
            }
        }
        public async Task RemoveBlockFromDatabaseAsync(string databaseName, string blockHash)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var update = Builders<TransactionDB>.Update.Set<string>("BlockHash", "");
                await transactionCollection.UpdateManyAsync(t => t.BlockHash == blockHash, update);

                await blockheaderCollection.DeleteOneAsync(b => b.BlockHash == blockHash);
                await session.CommitTransactionAsync();
            }
        }
        public async Task RemoveUnconfirmedBlocks(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                var query = from b in blockheaderCollection.AsQueryable()
                            where b.Confirmed == false
                            select b.BlockHash;

                var unconfirmedBlockhashes = await query.ToListAsync();
                foreach (var blockhash in unconfirmedBlockhashes)
                {
                    var update = Builders<TransactionDB>.Update.Set<string>("BlockHash", "");
                    await transactionCollection.UpdateManyAsync(t => t.BlockHash == blockhash, update);
                }
                await blockheaderCollection.DeleteManyAsync(b => b.Confirmed == false);
                await session.CommitTransactionAsync();
            }
        }

        public async Task<Block> GetSidechainBlockAsync(string databaseName, string blockhash)
        {
            databaseName = ClearSpecialCharacters(databaseName);

            var blockheader = await GetBlockHeaderByBlockHashAsync(databaseName, blockhash);

            if (blockheader == null) return null;

            var transactionList = await GetBlockTransactionsAsync(databaseName, blockhash);

            return new Block(blockheader.BlockHeaderFromBlockHeaderDB(), transactionList);
        }

        public async Task<bool> IsBlockInDatabase(string databaseName, string blockhash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            var blockheader = await GetBlockHeaderByBlockHashAsync(databaseName, blockhash);
            return blockheader != null;
        }

        public async Task<IList<Block>> GetSidechainBlocksSinceSequenceNumberAsync(string databaseName, ulong beginSequenceNumber, ulong endSequenceNumber)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockHeaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var blockHeaders = await (await blockHeaderCollection.FindAsync(b => b.SequenceNumber >= beginSequenceNumber && b.SequenceNumber <= endSequenceNumber)).ToListAsync();
                var orderedBlockHeaders = blockHeaders.OrderByDescending(b => b.SequenceNumber);
                var blockList = new List<Block>();
                foreach (var blockHeader in orderedBlockHeaders)
                {
                    var transactionList = await GetBlockTransactionsAsync(databaseName, blockHeader.BlockHash);
                    blockList.Add(new Block(blockHeader.BlockHeaderFromBlockHeaderDB(), transactionList));
                }
                return blockList;
            }
        }

        public async Task<IEnumerable<ulong>> GetMissingBlockNumbers(string databaseName, ulong endSequenceNumber)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                ulong lowerEndToGet = 1;
                ulong upperEndToGet;
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockHeaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var missingSequenceNumbers = new List<ulong>();

                //TODO rpinto - why is this done in a while true loop
                while (true)
                {
                    upperEndToGet = lowerEndToGet + 99 > endSequenceNumber ? endSequenceNumber : lowerEndToGet + 99;

                    var blockHeadersResponse = await blockHeaderCollection.FindAsync(b => b.SequenceNumber >= lowerEndToGet && b.SequenceNumber <= upperEndToGet);
                    var blockHeaders = await blockHeadersResponse.ToListAsync();
                    var sequenceNumbers = blockHeaders.Select(b => b.SequenceNumber).OrderBy(s => s);
                    for (ulong i = lowerEndToGet; i <= upperEndToGet; i++)
                    {
                        if (!sequenceNumbers.Contains(i)) missingSequenceNumbers.Add(i);
                    }

                    if (upperEndToGet == endSequenceNumber) break;

                    lowerEndToGet = upperEndToGet + 1;
                }

                return missingSequenceNumbers.OrderByDescending(s => s);
            }
        }

        //TODO rpinto - there could be a problem here. If there is a new block that is about to be confirmed but
        //hasn't yet, this method will destroy the relation of all transactions related to that block
        //but the block may still be accepted and then there would be transactions doubled
        //If the smart contract validates the last transaction sequence number this problem may be prevented
        //The provider would just produce a wrong block, but even so he would have to recover the missing transactions,
        //or reassociate them. I don't think this method is doing that
        public async Task<bool> TrySynchronizeDatabaseWithSmartContract(string databaseName, string lastConfirmedSmartContractBlockHash, long lastProductionStartTime, ProducerTypeEnum producerType)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            //gets the latest confirmed block
            var blockheaderDB = await GetBlockHeaderByBlockHashAsync(databaseName, lastConfirmedSmartContractBlockHash);
            if (blockheaderDB != null)
            {
                using (IClientSession session = await MongoClient.StartSessionAsync())
                {
                    session.StartTransaction();

                    var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                    var blockHeaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                    var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                    //marciak - this is checking if there's unconfirmed blocks
                    var blockHeaderQuery = from b in blockHeaderCollection.AsQueryable()
                                           where
                                           //all blocks with timestamps earlier than when the last production start time
                                           b.Timestamp < (ulong)lastProductionStartTime
                                           //and unconfirmed
                                           && b.Confirmed == false
                                           select b;

                    //marciak - this is disassociating the transactions from the unconfirmed blocks, so that they can be associated to new blocks
                    foreach (var blockHeaderDBToRemove in blockHeaderQuery.AsEnumerable())
                    {
                        var update = Builders<TransactionDB>.Update.Set<string>("BlockHash", "");
                        await transactionCollection.UpdateManyAsync(t => t.BlockHash == blockHeaderDBToRemove.BlockHash, update);
                    }

                    await blockHeaderCollection.DeleteManyAsync(b => b.Timestamp < (ulong)lastProductionStartTime && b.Confirmed == false);

                    var numberOfBlocks = await blockHeaderCollection.CountDocumentsAsync(new BsonDocument());

                    await session.CommitTransactionAsync();

                    //TODO rpinto changed it to be bigger or equal to the number of blocks
                    //old version - if (Convert.ToInt64(blockheaderDB.SequenceNumber) == numberOfBlocks)

                    if (numberOfBlocks >= Convert.ToInt64(blockheaderDB.SequenceNumber))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<bool> IsBlockConfirmed(string databaseName, string blockHash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            var blockHeader = await GetBlockHeaderByBlockHashAsync(databaseName, blockHash);
            return blockHeader != null ? blockHeader.Confirmed : false;
        }
        public async Task ConfirmBlock(string databaseName, string blockHash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            var blockHeader = await GetBlockHeaderByBlockHashAsync(databaseName, blockHash);
            if (blockHeader == null) return;

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var validBlockHeaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                blockHeader.Confirmed = true;
                await validBlockHeaderCollection.ReplaceOneAsync(b => b.BlockHash == blockHash, blockHeader);
            }
        }

        public async Task ClearValidatorNode(string databaseName, string blockHash, uint transactionCount)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            var blockHeader = await GetBlockHeaderByBlockHashAsync(databaseName, blockHash);
            if (blockHeader == null) return;

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockHeaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);

                await blockHeaderCollection.DeleteManyAsync(b => b.SequenceNumber < blockHeader.SequenceNumber);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                await transactionCollection.DeleteManyAsync(t => t.BlockHash != blockHash);
            }
        }


        private async Task<BlockheaderDB> GetBlockHeaderByBlockHashAsync(string databaseName, string blockHash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);

                var blockheaderQuery = from b in blockheaderCollection.AsQueryable()
                                       where b.BlockHash == blockHash
                                       select b;

                return await blockheaderQuery.SingleOrDefaultAsync();
            }
        }
        public async Task<IList<Transaction>> GetBlockTransactionsAsync(string databaseName, string blockhash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       where t.BlockHash == blockhash
                                       orderby t.SequenceNumber
                                       select t;

                return (await transactionQuery.ToListAsync()).Select(t => t.TransactionFromTransactionDB()).ToList();
            }
        }

        public async Task<Transaction> GetTransactionBySequenceNumber(string databaseName, ulong transactionNumber)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       where t.SequenceNumber == transactionNumber
                                       select t;

                return (await transactionQuery.ToListAsync()).Select(t => t.TransactionFromTransactionDB()).SingleOrDefault();
            }
        }

        public async Task<IList<Transaction>> GetTransactionsSinceSequenceNumber(string databaseName, ulong transactionNumber)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       where t.SequenceNumber > transactionNumber
                                       select t;

                return (await transactionQuery.ToListAsync()).Select(t => t.TransactionFromTransactionDB()).ToList();
            }
        }

        public async Task<TransactionDB> GetTransactionDBAsync(string databaseName, string transactionHash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       where t.TransactionHash == transactionHash
                                       select t;

                return (await transactionQuery.SingleOrDefaultAsync());
            }
        }





        public async Task<bool> IsTransactionInDB(string databaseName, Transaction transaction)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var transactionQuery = from t in transactionCollection.AsQueryable()
                                       where t.TransactionHash == HashHelper.ByteArrayToFormattedHexaString(transaction.TransactionHash)
                                       select t;

                if (await transactionQuery.SingleOrDefaultAsync() == null) return false;
                return true;
            }
        }
        public async Task SaveTransaction(string databaseName, Transaction transaction)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);

                await transactionCollection.InsertOneAsync(transactionDB);

            }
        }
        public async Task<IList<Transaction>> RetrieveTransactionsInMempool(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            return await RetrieveTransactionsInMempool(databaseName, MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
        }

        #region Recover DB

        public async Task AddProducingSidechainToDatabaseAsync(string sidechain, ulong timestamp, bool isAutomatic, int producerType)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            var sidechainDb = new SidechainDB()
            {
                Id = sidechain,
                Timestamp = timestamp,
                ProducerType = producerType,
                IsAutomatic = isAutomatic
            };

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var sidechainCollection = recoverDatabase.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME);
                await sidechainCollection.InsertOneAsync(sidechainDb);
            }
        }

        public async Task RemoveProducingSidechainFromDatabaseAsync(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                if ((await sidechainDatabase.ListCollectionsAsync()).ToList().Count() == 0)
                {
                    await MongoClient.DropDatabaseAsync(_dbPrefix + databaseName);
                }
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var sidechainCollection = recoverDatabase.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME);
                await sidechainCollection.DeleteOneAsync(s => s.Id == databaseName);
                await session.CommitTransactionAsync();
            }
        }

        public async Task<bool> CheckIfProducingSidechainAlreadyExists(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var sidechains = recoverDatabase.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME).AsQueryable();
                var query = from s in sidechains
                            where s.Id == databaseName
                            select s;

                var result = query.Any();
                return result;
            }
        }

        public async Task<IList<SidechainDB>> GetAllProducingSidechainsAsync()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var sidechains = recoverDatabase.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME).AsQueryable();
                var query = from s in sidechains
                            select s;

                var result = query.ToList();
                return result;
            }
        }

        public async Task<SidechainDB> GetProducingSidechainAsync(string sidechain, ulong timestamp)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var sidechains = recoverDatabase.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME).AsQueryable();
                var query = from s in sidechains
                            where s.Id == sidechain && s.Timestamp == timestamp
                            select s;

                var result = query.ToList();
                return result.SingleOrDefault();
            }
        }

        public async Task AddPastSidechainToDatabaseAsync(string sidechain, ulong timestamp, bool alreadyLeft = false, string reasonLeft = null)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            var pastSidechainDb = new PastSidechainDB()
            {
                Sidechain = sidechain,
                Timestamp = timestamp,
                AlreadyLeft = alreadyLeft,
                DateLeftTimestamp = alreadyLeft ? Convert.ToUInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) : 0,
                ReasonLeft = reasonLeft
            };

            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var sidechainCollection = recoverDatabase.GetCollection<PastSidechainDB>(MongoDbConstants.PAST_SIDECHAINS_COLLETION_NAME);

                var existingPastSidechain = sidechainCollection.AsQueryable<PastSidechainDB>().Where(s => s.Sidechain == sidechain && s.Timestamp == timestamp).SingleOrDefault();
                if (existingPastSidechain != null) pastSidechainDb.ReasonLeft = existingPastSidechain.ReasonLeft;

                await sidechainCollection.ReplaceOneAsync(s => s.Sidechain == sidechain && s.Timestamp == timestamp, pastSidechainDb, new UpdateOptions { IsUpsert = true });
            }
        }

        public async Task RemovePastSidechainFromDatabaseAsync(string sidechain, ulong timestamp)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var sidechainCollection = recoverDatabase.GetCollection<PastSidechainDB>(MongoDbConstants.PAST_SIDECHAINS_COLLETION_NAME);
                await sidechainCollection.DeleteOneAsync(s => s.Sidechain == sidechain && s.Timestamp == timestamp);
            }
        }

        public async Task<IList<PastSidechainDB>> GetAllPastSidechainsAsync()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var sidechains = recoverDatabase.GetCollection<PastSidechainDB>(MongoDbConstants.PAST_SIDECHAINS_COLLETION_NAME).AsQueryable();
                var query = from s in sidechains
                            select s;

                var result = query.ToList();
                return result;
            }
        }

        public async Task<PastSidechainDB> GetPastSidechainAsync(string sidechain, ulong timestamp)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var sidechains = recoverDatabase.GetCollection<PastSidechainDB>(MongoDbConstants.PAST_SIDECHAINS_COLLETION_NAME).AsQueryable();
                var query = from s in sidechains
                            where s.Sidechain == sidechain && s.Timestamp == timestamp
                            select s;

                var result = query.ToList();
                return result.SingleOrDefault();
            }
        }

        #endregion

        public async Task<TransactionDB> GetTransactionToExecute(string sidechain)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + sidechain);

                var transactionCol = database.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME).AsQueryable();
                var query = from t in transactionCol
                            select t;

                return await query.SingleOrDefaultAsync();

            }
        }

        public async Task UpdateTransactionToExecute(string sidechain, TransactionDB transaction)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                var database = MongoClient.GetDatabase(_dbPrefix + sidechain);

                var transactionCol = database.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME);

                await transactionCol.DeleteManyAsync(t => true);

                await transactionCol.InsertOneAsync(transaction);

                await session.CommitTransactionAsync();
            }
        }
    }
}
