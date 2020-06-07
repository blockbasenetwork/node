using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Protos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static BlockBase.Network.Rounting.MessageForwarder;
using BlockBase.Runtime.Helpers;

namespace BlockBase.Runtime.Network
{
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
        private ConcurrentDictionary<string, SemaphoreSlim> _validatorSemaphores;
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

            _validatorSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _networkService.SubscribeMinedBlockReceivedEvent(MessageForwarder_MinedBlockReceived);
            _blockSender = blockSender;
        }

        public async Task HandleReceivedBlock(SidechainPool sidechainPool, BlockProto blockProtoReceived)
        {
            // var sidechainName = sidechainPool.SmartContractAccount;
            var databaseName = sidechainPool.ClientAccountName;

            var sidechainSemaphore = TryGetAndAddSidechainSemaphore(sidechainPool.ClientAccountName);

            await sidechainSemaphore.WaitAsync();
            try
            {
                var blockReceived = new Block().SetValuesFromProto(blockProtoReceived);

                var blockHashString = HashHelper.ByteArrayToFormattedHexaString(blockReceived.BlockHeader.BlockHash);

                if (await AlreadyProcessedThisBlock(databaseName, blockHashString)) return;
                if (!await IsTimeForThisProducerToProduce(sidechainPool, blockReceived.BlockHeader.Producer)) return;

                var i = 0;
                while (i < 3)
                {
                    try
                    {
                        BlockHeader blockheader = (await _mainchainService.GetLastSubmittedBlockheader(sidechainPool.ClientAccountName, (int)sidechainPool.BlocksBetweenSettlement)).ConvertToBlockHeader();

                        if (ValidationHelper.ValidateBlockAndBlockheader(blockReceived, sidechainPool, blockheader, _logger, out byte[] trueBlockHash) && await ValidateBlockTransactions(blockReceived, sidechainPool))
                        {
                           
                            await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(blockReceived, databaseName);
                            
                            await _blockSender.SendBlockToSidechainMembers(sidechainPool, blockProtoReceived, _endPoint);

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
            finally
            {
                sidechainSemaphore.Release();
            }

        }

        private async Task<bool> AlreadyProcessedThisBlock(string sidechain, string blockhash)
        {
            if (await _mongoDbProducerService.GetSidechainBlockAsync(sidechain, blockhash) != null)
            {
                //_logger.LogDebug("Block already received and its a valid Block.");
                return true;
            }
            //_logger.LogDebug("Blockhash: " + blockhash + " not processed yet.");
            return false;
        }

        private async Task<bool> IsTimeForThisProducerToProduce(SidechainPool sidechainPool, string blockProducer)
        {
            var currentProducerTable = await _mainchainService.RetrieveCurrentProducer(sidechainPool.ClientAccountName);
            if (currentProducerTable.Producer == blockProducer) return true;
            return false;
        }

        private async void MessageForwarder_MinedBlockReceived(BlockReceivedEventArgs args)
        {
            try
            {
                var blockProto = SerializationHelper.DeserializeBlock(args.BlockBytes, _logger);
                if (blockProto == null) return;

                if (!_sidechainKeeper.TryGet(args.ClientAccountName, out var sidechainContext))
                {
                    _logger.LogDebug($"Block received but sidechain {args.ClientAccountName} is unknown.");
                    return;
                }

                
                var sidechainPool = sidechainContext.SidechainPool;

                var isProductionTime = (await _mainchainService.RetrieveContractState(sidechainPool.ClientAccountName)).ProductionTime;

                if (!isProductionTime)
                {
                    _logger.LogDebug($"Mined block received but it's not mining time.");
                    return;
                }

                //TODO rpinto - may throw an exception if currentproduceris null
                var startProductionTime = (await _mainchainService.RetrieveCurrentProducer(sidechainPool.ClientAccountName)).StartProductionTime;

                var lastValidBlockheaderSmartContract = await _mainchainService.GetLastValidSubmittedBlockheader(sidechainPool.ClientAccountName, (int)sidechainPool.BlocksBetweenSettlement);

                if (sidechainPool.ProducerType != ProducerTypeEnum.Validator && lastValidBlockheaderSmartContract != null && !await _mongoDbProducerService.IsBlockConfirmed(sidechainPool.ClientAccountName, lastValidBlockheaderSmartContract.BlockHash))
                {
                    _logger.LogDebug($"Mined block received but producer is not up to date.");
                    return;
                }

                await HandleReceivedBlock(sidechainPool, blockProto);
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

        private async Task<bool> ValidateBlockTransactions(Block block, SidechainPool sidechain)
        {
            ulong lastSequenceNumber = (await _mongoDbProducerService.LastIncludedTransaction(sidechain.ClientAccountName))?.SequenceNumber ?? 0;
            foreach (var transaction in block.Transactions)
            {
                if (transaction.SequenceNumber != ++lastSequenceNumber)
                {
                    _logger.LogDebug($"Block #{block.BlockHeader.SequenceNumber} Transaction #{transaction.SequenceNumber} doesn't follow order from last sequence number #{lastSequenceNumber}");
                    return false;
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

        #region Semaphore Helpers

        private SemaphoreSlim TryGetAndAddSidechainSemaphore(string sidechain)
        {
            var semaphoreKeyPair = _validatorSemaphores.FirstOrDefault(s => s.Key == sidechain);

            var defaultKeyValuePair = default(KeyValuePair<string, SemaphoreSlim>);
            if (semaphoreKeyPair.Equals(defaultKeyValuePair))
            {
                var newSemaphore = new SemaphoreSlim(1, 1);
                _validatorSemaphores.TryAdd(sidechain, newSemaphore);

                return newSemaphore;
            }

            return semaphoreKeyPair.Value;
        }
        #endregion
    }
}