﻿using MongoDB.Driver;
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

        public async Task CreateCollections(string databaseName)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                try
                {
                    var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);

                    if (!(await CollectionExistsAsync(databaseName, MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME)))
                        await sidechainDatabase.CreateCollectionAsync(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                    if (!(await CollectionExistsAsync(databaseName, MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME)))
                        await sidechainDatabase.CreateCollectionAsync(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                    if (!(await CollectionExistsAsync(databaseName, MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME)))
                        await sidechainDatabase.CreateCollectionAsync(MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME);
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to create collections");
                    _logger.LogDebug($"Exception {e}");
                }
            }
        }

        public async Task AddBlockToSidechainDatabaseAsync(Block block, string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (var session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                var blockHeaderDB = new BlockheaderDB().BlockheaderDBFromBlockHeader(block.BlockHeader);

                if (IsReplicaSet())
                {
                    session.StartTransaction();
                    try
                    {
                        await blockheaderCollection.InsertOneAsync(session, blockHeaderDB);

                        foreach (var transaction in block.Transactions)
                        {
                            var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
                            transactionDB.BlockHash = HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash);
                            await transactionCollection.ReplaceOneAsync(session, t => t.TransactionHash == transactionDB.TransactionHash, transactionDB, new UpdateOptions { IsUpsert = true });
                        }
                        await session.CommitTransactionAsync();
                    }
                    catch (Exception e)
                    {
                        await session.AbortTransactionAsync();
                        throw e;
                    }
                }
                else
                {
                    await blockheaderCollection.InsertOneAsync(blockHeaderDB);
                    foreach (var transaction in block.Transactions)
                    {
                        var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
                        transactionDB.BlockHash = HashHelper.ByteArrayToFormattedHexaString(block.BlockHeader.BlockHash);
                        await transactionCollection.ReplaceOneAsync(t => t.TransactionHash == transactionDB.TransactionHash, transactionDB, new UpdateOptions { IsUpsert = true });
                    }
                }
            }
        }
        public async Task RemoveBlockFromDatabaseAsync(string databaseName, string blockHash)
        {
            databaseName = ClearSpecialCharacters(databaseName);
            using (var session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                var update = Builders<TransactionDB>.Update.Set<string>("BlockHash", "");

                if (IsReplicaSet())
                {
                    session.StartTransaction();
                    try
                    {
                        await transactionCollection.UpdateManyAsync(session, t => t.BlockHash == blockHash, update);
                        await blockheaderCollection.DeleteOneAsync(session, b => b.BlockHash == blockHash);
                        await session.CommitTransactionAsync();
                    }
                    catch (Exception e)
                    {
                        await session.AbortTransactionAsync();
                        throw e;
                    }
                }
                else
                {
                    await transactionCollection.UpdateManyAsync(t => t.BlockHash == blockHash, update);
                    await blockheaderCollection.DeleteOneAsync(b => b.BlockHash == blockHash);
                }
            }
        }
        public async Task RemoveUnconfirmedBlocks(string databaseName)
        {
            databaseName = ClearSpecialCharacters(databaseName);

            using (var session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                var blockheaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                var query = from b in blockheaderCollection.AsQueryable()
                            where b.Confirmed == false
                            select b.BlockHash;
                var unconfirmedBlockhashes = await query.ToListAsync();

                var update = Builders<TransactionDB>.Update.Set<string>("BlockHash", "");

                if (IsReplicaSet())
                {
                    session.StartTransaction();
                    try
                    {
                        foreach (var blockhash in unconfirmedBlockhashes)
                        {
                            await transactionCollection.UpdateManyAsync(session, t => t.BlockHash == blockhash, update);
                        }
                        await blockheaderCollection.DeleteManyAsync(session, b => b.Confirmed == false);
                        await session.CommitTransactionAsync();
                    }
                    catch (Exception e)
                    {
                        await session.AbortTransactionAsync();
                        throw e;
                    }
                }
                else
                {
                    foreach (var blockhash in unconfirmedBlockhashes)
                    {
                        await transactionCollection.UpdateManyAsync(t => t.BlockHash == blockhash, update);
                    }
                    await blockheaderCollection.DeleteManyAsync(b => b.Confirmed == false);
                }
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
                using (var session = await MongoClient.StartSessionAsync())
                {
                    var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                    var blockHeaderCollection = sidechainDatabase.GetCollection<BlockheaderDB>(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                    var transactionCollection = sidechainDatabase.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);

                    //marciak - this is checking if there's unconfirmed blocks
                    var blockHeaderQuery = from b in blockHeaderCollection.AsQueryable()
                                           where
                                           //all blocks with timestamps earlier than when the last production start time
                                           b.Timestamp < (ulong)lastProductionStartTime
                                           //and unconfirmed
                                           && (b.SequenceNumber > blockheaderDB.SequenceNumber || !b.Confirmed)
                                           select b;

                    var update = Builders<TransactionDB>.Update.Set<string>("BlockHash", "");

                    if (IsReplicaSet())
                    {
                        session.StartTransaction();
                        try
                        {
                            //marciak - this is disassociating the transactions from the unconfirmed blocks, so that they can be associated to new blocks
                            foreach (var blockHeaderDBToRemove in blockHeaderQuery.AsEnumerable())
                            {
                                await transactionCollection.UpdateManyAsync(session, t => t.BlockHash == blockHeaderDBToRemove.BlockHash, update);
                            }
                            await blockHeaderCollection.DeleteManyAsync(session, b => b.Timestamp < (ulong)lastProductionStartTime && (b.SequenceNumber > blockheaderDB.SequenceNumber || !b.Confirmed));

                            await session.CommitTransactionAsync();
                        }
                        catch (Exception e)
                        {
                            await session.AbortTransactionAsync();
                            throw e;
                        }
                    }
                    else
                    {
                        foreach (var blockHeaderDBToRemove in blockHeaderQuery.AsEnumerable())
                        {
                            await transactionCollection.UpdateManyAsync(t => t.BlockHash == blockHeaderDBToRemove.BlockHash, update);
                        }
                        await blockHeaderCollection.DeleteManyAsync(b => b.Timestamp < (ulong)lastProductionStartTime && (b.SequenceNumber > blockheaderDB.SequenceNumber || !b.Confirmed));
                    }

                    var numberOfBlocks = await blockHeaderCollection.CountDocumentsAsync(new BsonDocument());
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


        public async Task<BlockheaderDB> GetBlockHeaderByBlockHashAsync(string databaseName, string blockHash)
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
                                       where t.SequenceNumber == Convert.ToInt64(transactionNumber)
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
                                       where t.SequenceNumber > Convert.ToInt64(transactionNumber)
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
            using (var session = await MongoClient.StartSessionAsync())
            {
                var sidechainDatabase = MongoClient.GetDatabase(_dbPrefix + databaseName);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.BLOCKHEADERS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.PROVIDER_TRANSACTIONS_COLLECTION_NAME);
                await sidechainDatabase.DropCollectionAsync(MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME);

                if ((await sidechainDatabase.ListCollectionsAsync()).ToList().Count() == 0)
                {
                    await MongoClient.DropDatabaseAsync(_dbPrefix + databaseName);
                }
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var sidechainCollection = recoverDatabase.GetCollection<SidechainDB>(MongoDbConstants.PRODUCING_SIDECHAINS_COLLECTION_NAME);
                await sidechainCollection.DeleteOneAsync(s => s.Id == databaseName);
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

                if (existingPastSidechain == null)
                {
                    await sidechainCollection.InsertOneAsync(pastSidechainDb);
                }
                else
                {
                    pastSidechainDb.ReasonLeft = existingPastSidechain.ReasonLeft;
                    pastSidechainDb._id = existingPastSidechain._id;
                    await sidechainCollection.FindOneAndReplaceAsync(s => s.Sidechain == sidechain && s.Timestamp == timestamp, pastSidechainDb);
                }
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

        public async Task<TransactionDB> GetTransactionToExecute(string sidechain, long lastTransactionSequenceNumber)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + sidechain);

                var transactionCol = database.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME).AsQueryable();
                var query = from t in transactionCol
                            where t.SequenceNumber <= lastTransactionSequenceNumber
                            select t;

                return await query.SingleOrDefaultAsync();

            }
        }

        public async Task UpdateTransactionToExecute(string sidechain, TransactionDB transaction)
        {
            sidechain = ClearSpecialCharacters(sidechain);
            using (var session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + sidechain);
                var transactionCol = database.GetCollection<TransactionDB>(MongoDbConstants.PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME);

                if (IsReplicaSet())
                {
                    session.StartTransaction();
                    try
                    {
                        await transactionCol.DeleteManyAsync(session, t => true);
                        await transactionCol.InsertOneAsync(session, transaction);

                        await session.CommitTransactionAsync();
                    }
                    catch (Exception e)
                    {
                        await session.AbortTransactionAsync();
                        throw e;
                    }
                }
                else
                {
                    await transactionCol.DeleteManyAsync(t => true);
                    await transactionCol.InsertOneAsync(transaction);
                }
            }
        }

        public async Task AddProviderMinValuesToDatabaseAsync(ProviderMinValuesDB providerminValues)
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var recoverDatabase = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                var providerMinValuesCol = recoverDatabase.GetCollection<ProviderMinValuesDB>(MongoDbConstants.PROVIDER_MIN_VALUES_COLLETION_NAME);
                await providerMinValuesCol.InsertOneAsync(providerminValues);
            }
        }

        public async Task<ProviderMinValuesDB> GetLatestProviderMinValues()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var providerMinValuesCol = database.GetCollection<ProviderMinValuesDB>(MongoDbConstants.PROVIDER_MIN_VALUES_COLLETION_NAME).AsQueryable();
                var query = from t in providerMinValuesCol
                            orderby t.Timestamp descending
                            select t;

                return query.FirstOrDefault();
            }
        }

        public async Task<ProviderMinValuesDB> GetFirstProviderMinValues()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);

                var providerMinValuesCol = database.GetCollection<ProviderMinValuesDB>(MongoDbConstants.PROVIDER_MIN_VALUES_COLLETION_NAME).AsQueryable();
                var query = from t in providerMinValuesCol
                            orderby t.Timestamp
                            select t;

                return query.FirstOrDefault();
            }
        }

        public async Task DropProviderMinValues()
        {
            using (IClientSession session = await MongoClient.StartSessionAsync())
            {
                var database = MongoClient.GetDatabase(_dbPrefix + MongoDbConstants.RECOVER_DATABASE_NAME);
                await database.DropCollectionAsync(MongoDbConstants.PROVIDER_MIN_VALUES_COLLETION_NAME);
            }
        }
    }
}
