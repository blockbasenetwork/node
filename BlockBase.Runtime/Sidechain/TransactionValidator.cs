using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Protos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Rounting;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain.Helpers;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlockBase.Network.IO;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using BlockBase.Network.IO.Enums;

namespace BlockBase.Runtime.Sidechain
{
    public class TransactionValidator
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private SidechainKeeper _sidechainKeeper;
        private IMongoDbProducerService _mongoDbProducerService;
        private IMainchainService _mainChainService;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ConcurrentDictionary<string, SemaphoreSlim> _validatorSemaphores;

        public TransactionValidator(ILogger<TransactionValidator> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, INetworkService networkService, SidechainKeeper sidechainKeeper, IMongoDbProducerService mongoDbProducerService, IMainchainService mainChainService)
        {
            _logger = logger;
            _logger.LogDebug("Creating transaction validator.");
            _networkService = networkService;
            _networkService.SubscribeTransactionReceivedEvent(MessageForwarder_TransactionsReceived);
            _sidechainKeeper = sidechainKeeper;
            _mongoDbProducerService = mongoDbProducerService;
            _mainChainService = mainChainService;
            _nodeConfigurations = nodeConfigurations.Value;
            _validatorSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _networkConfigurations = networkConfigurations.Value;
        }

        private async void MessageForwarder_TransactionsReceived(MessageForwarder.TransactionsReceivedEventArgs args, IPEndPoint sender)
        {            
            var transactionsProto = SerializationHelper.DeserializeTransactions(args.TransactionsBytes, _logger);
            
            if (transactionsProto == null) return;
            
            foreach(var transactionProto in transactionsProto) 
                await ValidateEachTransaction(transactionProto, args.ClientAccountName, sender);
        }

        private async Task ValidateEachTransaction(TransactionProto transactionProto, string clientAccountName, IPEndPoint sender)
        {
            var transaction = new Transaction().SetValuesFromProto(transactionProto);
            _logger.LogDebug($"TRANSACTION {transaction.SequenceNumber} RECEIVED");
            // _logger.LogDebug(transaction.BlockHash.ToString() + ":" + transaction.DatabaseName + ":" + transaction.SequenceNumber + ":" + transaction.Json + ":" + transaction.Signature + ":" + transaction.Timestamp);

            var sidechainPoolValuePair = _sidechainKeeper.Sidechains.FirstOrDefault(s => s.Key == clientAccountName);

            var defaultKeyValuePair = default(KeyValuePair<string, SidechainPool>);

            if (sidechainPoolValuePair.Equals(defaultKeyValuePair))
            {
                _logger.LogDebug($"Transaction received but sidechain {clientAccountName} is unknown.");
                return;
            }

            var sidechainPool = sidechainPoolValuePair.Value;

            var sidechainSemaphore = TryGetAndAddSidechainSemaphore(sidechainPool.ClientAccountName);

            await sidechainSemaphore.WaitAsync();

             var message = new NetworkMessage(
                    NetworkMessageTypeEnum.ConfirmTransactionReception, 
                    BitConverter.GetBytes(transaction.SequenceNumber),
                    TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, 
                    _nodeConfigurations.ActivePublicKey, 
                    _networkConfigurations.LocalIpAddress + ":" + _networkConfigurations.LocalTcpPort, 
                    _nodeConfigurations.AccountName, sender);

            try
            {
                if (!ValidationHelper.IsTransactionHashValid(transaction, out byte[] transactionHash))
                {
                    _logger.LogDebug($"Transaction hash not valid.");
                    return;
                }
                var databaseName = clientAccountName;
                if (await _mongoDbProducerService.IsTransactionInDB(databaseName, transaction))
                {
                    _logger.LogDebug($"Already have transaction with same transaction hash or same sequence number.");
                    await _networkService.SendMessageAsync(message);
                    return;
                }

                if (!SignatureHelper.VerifySignature(sidechainPool.ClientPublicKey, transaction.Signature, transactionHash))
                {
                    _logger.LogDebug($"Transaction signature not valid.");
                    return;
                }

                _logger.LogDebug($"Saving transaction.");
                
                await _mongoDbProducerService.SaveTransaction(databaseName, transaction);

               
                
                _logger.LogDebug("Sending confirmation transaction.");
                await _networkService.SendMessageAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Transaction validator crashed with exception {e.Message}");
            }
            finally
            {
                sidechainSemaphore.Release();
            }

        }

        //TODO:REFACTOR
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