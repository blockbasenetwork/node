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
using BlockBase.Domain.Eos;
using BlockBase.Domain.Protos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.ProducerData.MongoDbEntities;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain.Helpers;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static BlockBase.Network.Rounting.MessageForwarder;

namespace BlockBase.Runtime.Sidechain
{
    public class BlockValidator
    {
        private NodeConfigurations _nodeConfigurations;
        private ILogger _logger;
        private IMongoDbProducerService _mongoDbProducerService;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private SidechainKeeper _sidechainKeeper;
        private NetworkConfigurations _networkConfigurations;
        private BlockSender _blockSender;
        private ConcurrentDictionary<string, SemaphoreSlim> _validatorSemaphores;
        private string _endPoint;


        public BlockValidator(SystemConfig systemConfig, IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations, ILogger<BlockValidator> logger, INetworkService networkService, IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, SidechainKeeper sidechainKeeper, BlockSender blockSender)
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
                        BlockHeader blockheader = (await _mainchainService.GetLastSubmittedBlockheader(sidechainPool.ClientAccountName)).ConvertToBlockHeader();

                        if (ValidationHelper.ValidateBlockAndBlockheader(blockReceived, sidechainPool, blockheader, _logger, out byte[] trueBlockHash) && await ValidateBlockTransactions(blockReceived, sidechainPool))
                        {
                            await _mongoDbProducerService.AddBlockToSidechainDatabaseAsync(blockReceived, databaseName);
                            await _blockSender.SendBlockToSidechainMembers(sidechainPool, blockProtoReceived, _endPoint);

                            var proposal = await _mainchainService.RetrieveProposal(blockReceived.BlockHeader.Producer, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);
                            if (proposal != null) await TryApproveTransaction(blockReceived.BlockHeader.Producer, proposal);
                            break;
                        }
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
            var currentProducerTable = (await _mainchainService.RetrieveCurrentProducer(sidechainPool.ClientAccountName)).SingleOrDefault();
            if (currentProducerTable.Producer == blockProducer) return true;
            return false;
        }

        private async void MessageForwarder_MinedBlockReceived(BlockReceivedEventArgs args)
        {
            var blockProto = SerializationHelper.DeserializeBlock(args.BlockBytes, _logger);
            if (blockProto == null) return;

            var sidechainPoolValuePair = _sidechainKeeper.Sidechains.SingleOrDefault(s => s.Key == args.ClientAccountName);

            var defaultKeyValuePair = default(KeyValuePair<string, SidechainPool>);
            if (sidechainPoolValuePair.Equals(defaultKeyValuePair))
            {
                _logger.LogDebug($"Block received but sidechain {args.ClientAccountName} is unknown.");
                return;
            }

            var isProductionTime = (await _mainchainService.RetrieveContractState(sidechainPoolValuePair.Value.ClientAccountName)).ProductionTime;

            if (!isProductionTime)
            {
                _logger.LogDebug($"Mined block received but it's not mining time.");
                return;
            }

            var startProductionTime = (await _mainchainService.RetrieveCurrentProducer(sidechainPoolValuePair.Value.ClientAccountName)).SingleOrDefault().StartProductionTime;

            var lastValidBlockheaderSmartContractFromLastProduction = await _mainchainService.GetLastValidSubmittedBlockheaderFromLastProduction(sidechainPoolValuePair.Value.ClientAccountName, startProductionTime);

            if (lastValidBlockheaderSmartContractFromLastProduction != null && !await _mongoDbProducerService.IsBlockConfirmed(sidechainPoolValuePair.Value.ClientAccountName, lastValidBlockheaderSmartContractFromLastProduction.BlockHash))
            {
                _logger.LogDebug($"Mined block received but producer is not up to date.");
                return;
            }

            await HandleReceivedBlock(sidechainPoolValuePair.Value, blockProto);
        }

        private async Task TryApproveTransaction(string proposer, TransactionProposal proposal)
        {
            try
            {
                await _mainchainService.ApproveTransaction(proposer, proposal.ProposalName, _nodeConfigurations.AccountName, proposal.TransactionHash);
            }
            catch(ApiErrorException)
            {
                _logger.LogInformation("Unable to approve transaction, proposed transaction might have already been executed");
            }
        }

        private async Task<bool> ValidateBlockTransactions(Block block, SidechainPool sidechain)
        {
            ulong lastSequenceNumber = (await _mongoDbProducerService.LastIncludedTransaction(sidechain.ClientAccountName))?.SequenceNumber ?? 0;
            foreach(var transaction in block.Transactions)
            {
                if(transaction.SequenceNumber != ++lastSequenceNumber) return false;
                if(!ValidationHelper.IsTransactionHashValid(transaction, out byte[] transactionHash)) return false;
                if(!SignatureHelper.VerifySignature(sidechain.ClientPublicKey, transaction.Signature, transactionHash)) return false;
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