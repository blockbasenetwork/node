using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Protos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.Data;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Google.Protobuf;
using static BlockBase.Network.Rounting.MessageForwarder;
using BlockBase.Runtime.Helpers;
using BlockBase.Runtime.Provider;
using BlockBase.Utils.Threading;

namespace BlockBase.Runtime.Network
{
    //TODO rpinto - this whole class can be done better
    public class BlockValidationsHandler
    {
        private NodeConfigurations _nodeConfigurations;
        private ILogger _logger;
        private IMongoDbProducerService _mongoDbProducerService;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private SidechainKeeper _sidechainKeeper;
        private NetworkConfigurations _networkConfigurations;
        private BlockRequestsHandler _blockSender;
        private ThreadSafeList<string> _blocksBeingHandled;
        private string _endPoint;


        public BlockValidationsHandler(SystemConfig systemConfig, IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations, ILogger<BlockValidationsHandler> logger, INetworkService networkService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, SidechainKeeper sidechainKeeper, BlockRequestsHandler blockSender)
        {
            _logger = logger;
            _mongoDbProducerService = mongoDbProducerService;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _sidechainKeeper = sidechainKeeper;
            _networkConfigurations = networkConfigurations.Value;
            _nodeConfigurations = nodeConfigurations?.Value;
            _endPoint = systemConfig.IPAddress + ":" + systemConfig.TcpPort;

            _blocksBeingHandled = new ThreadSafeList<string>();
            _networkService.SubscribeMinedBlockReceivedEvent(MessageForwarder_ProducedBlockReceived);
            _blockSender = blockSender;
        }

        //TODO rpinto - this code may fail and needs to be refactored
        private async Task HandleReceivedBlock(SidechainPool sidechainPool, BlockProto blockProtoReceived)
        {
            // var sidechainName = sidechainPool.SmartContractAccount;
            var databaseName = sidechainPool.ClientAccountName;

            try
            {
                var blockReceived = new Block().SetValuesFromProto(blockProtoReceived);

                var blockHashString = HashHelper.ByteArrayToFormattedHexaString(blockReceived.BlockHeader.BlockHash);

                if (await _mongoDbProducerService.IsBlockInDatabase(databaseName, blockHashString)) return;
                if (!await IsTimeForThisProducerToProduce(sidechainPool, blockReceived.BlockHeader.Producer)) return;

                var i = 0;
                while (i < 3)
                {
                    try
                    {
                        BlockHeader blockheader = (await _mainchainService.GetLastSubmittedBlockheader(sidechainPool.ClientAccountName, (int)sidechainPool.BlocksBetweenSettlement)).ConvertToBlockHeader();
                        var previousBlock = await _mongoDbProducerService.GetBlockHeaderByBlockHashAsync(sidechainPool.ClientAccountName, HashHelper.ByteArrayToFormattedHexaString(blockheader.PreviousBlockHash));

                        if (ValidationHelper.ValidateBlockAndBlockheader(blockReceived, sidechainPool, blockheader, _logger, out byte[] trueBlockHash) && await ValidateBlockTransactions(blockReceived, sidechainPool, previousBlock?.LastTransactionSequenceNumber ?? 0))
                        {
                            _logger.LogDebug($"Adding block {blockReceived.BlockHeader.SequenceNumber} to database");
                            await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(blockReceived, databaseName);

                            // var proposal = await _mainchainService.RetrieveProposal(blockReceived.BlockHeader.Producer, sidechainPool.ClientAccountName);
                            // if (proposal != null) await TryApproveTransaction(blockReceived.BlockHeader.Producer, proposal);

                            var verifySignatures = await _mainchainService.RetrieveVerifySignatures(sidechainPool.ClientAccountName);
                            var producerVerifySignature = verifySignatures.FirstOrDefault(v => v.Account == blockReceived.BlockHeader.Producer);
                            if (producerVerifySignature != null) await TryAddSignature(sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, producerVerifySignature.BlockHash, producerVerifySignature.Transaction);

                            break;
                        }
                        i++;
                        await Task.Delay(150);
                    }
                    catch (Exception e)
                    {
                        i++;
                        _logger.LogCritical($"Failed try #{i} to approve received block");
                        _logger.LogDebug(e.ToString());
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Block Validator crashed with exception: {e}");
            }
        }

        private async Task<bool> IsTimeForThisProducerToProduce(SidechainPool sidechainPool, string blockProducer)
        {
            var currentProducerTable = await _mainchainService.RetrieveCurrentProducer(sidechainPool.ClientAccountName);
            if (currentProducerTable.Producer == blockProducer) return true;
            return false;
        }

        private async void MessageForwarder_ProducedBlockReceived(BlockReceivedEventArgs args)
        {
            try
            {
                var blockProto = SerializationHelper.DeserializeBlock(args.BlockBytes, _logger);
                if (blockProto == null) return;

                _logger.LogDebug($"Chain: {args.ClientAccountName} | Received block {blockProto.BlockHeader.SequenceNumber} from {blockProto.BlockHeader.Producer}");

                if (!_sidechainKeeper.TryGet(args.ClientAccountName, out var sidechainContext))
                {
                    _logger.LogDebug($"Block received but sidechain {args.ClientAccountName} is unknown.");
                    return;
                }

                var sidechainPool = sidechainContext.SidechainPool;
                var isProductionTime = (await _mainchainService.RetrieveContractState(sidechainPool.ClientAccountName)).ProductionTime;
                if (!isProductionTime)
                {
                    _logger.LogDebug($"Mined block received but it's not production time.");
                    return;
                }

                await _blockSender.SendBlockToSidechainMembers(sidechainPool, blockProto, _endPoint);

                var lastValidBlockheaderSmartContract = await _mainchainService.GetLastValidSubmittedBlockheader(sidechainPool.ClientAccountName, (int)sidechainPool.BlocksBetweenSettlement);

                if (sidechainPool.ProducerType != ProducerTypeEnum.Validator && lastValidBlockheaderSmartContract != null && !await _mongoDbProducerService.IsBlockConfirmed(sidechainPool.ClientAccountName, lastValidBlockheaderSmartContract.BlockHash))
                {
                    _logger.LogDebug($"Mined block received but producer is not up to date.");
                    return;
                }

                var blockKey = $"{args.ClientAccountName}|b{blockProto.BlockHeader.SequenceNumber}";
                if (!_blocksBeingHandled.GetEnumerable().Any(b => b == blockKey))
                {
                    _blocksBeingHandled.Add(blockKey);
                    await HandleReceivedBlock(sidechainPool, blockProto);
                    _blocksBeingHandled.Remove(blockKey);
                    _logger.LogDebug($"Chain: {args.ClientAccountName} | Finish handling block {blockProto.BlockHeader.SequenceNumber}");
                }
                else
                {
                    _logger.LogDebug($"Chain: {args.ClientAccountName} | Already handling block {blockProto.BlockHeader.SequenceNumber}");
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Failed to process received block with error: {e.Message}");
            }
        }

        private async Task TryApproveTransaction(string proposer, TransactionProposal proposal)
        {
            try
            {
                await _mainchainService.ApproveTransaction(proposer, proposal.ProposalName, _nodeConfigurations.AccountName, proposal.TransactionHash);
            }
            catch (ApiErrorException)
            {
                _logger.LogInformation("Unable to approve transaction, proposed transaction might have already been executed");
            }
        }

        private async Task TryAddSignature(string chain, string account, string blockHash, EosSharp.Core.Api.v1.Transaction transactionToSign)
        {
            try
            {
                await _mainchainService.SignVerifyTransactionAndAddToContract(chain, account, blockHash, transactionToSign);
            }
            catch (ApiErrorException)
            {
                _logger.LogInformation("Unable to add verify transaction signature");
            }
        }

        private async Task<bool> ValidateBlockTransactions(Block block, SidechainPool sidechain, ulong lastIncludedTransactionSequenceNumber)
        {
            foreach (var transaction in block.Transactions)
            {
                //_logger.LogDebug($"Chain: {sidechain.ClientAccountName} | Validating transaction #{transaction.SequenceNumber}");
                if (transaction.SequenceNumber != ++lastIncludedTransactionSequenceNumber)
                {
                    _logger.LogDebug($"Block #{block.BlockHeader.SequenceNumber} Transaction #{transaction.SequenceNumber} doesn't follow order from last sequence number #{lastIncludedTransactionSequenceNumber}");
                    return false;
                }
                //if already saved block, skip other validations
                if (await _mongoDbProducerService.IsTransactionInDB(sidechain.ClientAccountName, transaction))
                {
                    continue;
                }
                if (!ValidationHelper.IsTransactionHashValid(transaction, out byte[] transactionHash))
                {
                    _logger.LogDebug($"Transaction #{transaction.SequenceNumber} hash not valid");
                    return false;
                }
                if (!SignatureHelper.VerifySignature(sidechain.ClientPublicKey, transaction.Signature, transactionHash))
                {
                    _logger.LogDebug($"Transaction #{transaction.SequenceNumber} signature not valid");
                    return false;
                }
            }
            return true;
        }
    }
}