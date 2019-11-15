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
        private ConcurrentDictionary<string, SemaphoreSlim> _validatorSemaphores;

        public TransactionValidator(ILogger<TransactionValidator> logger, IOptions<NodeConfigurations> nodeConfigurations, INetworkService networkService, SidechainKeeper sidechainKeeper, IMongoDbProducerService mongoDbProducerService, IMainchainService mainChainService)
        {
            _logger = logger;
            _logger.LogDebug("Creating transaction validator.");
            _networkService = networkService;
            _networkService.SubscribeTransactionReceivedEvent(MessageForwarder_TransactionReceived);
            _sidechainKeeper = sidechainKeeper;
            _mongoDbProducerService = mongoDbProducerService;
            _mainChainService = mainChainService;
            _nodeConfigurations = nodeConfigurations.Value;
            _validatorSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        private async void MessageForwarder_TransactionReceived(MessageForwarder.TransactionReceivedEventArgs args, IPEndPoint sender)
        {
            _logger.LogDebug("TRANSACTION RECEIVED");
            var transactionProto = SerializationHelper.DeserializeTransaction(args.TransactionBytes, _logger);
            if (transactionProto == null) return;

            var transaction = new Transaction().SetValuesFromProto(transactionProto);

            var sidechainPoolValuePair = _sidechainKeeper.Sidechains.FirstOrDefault(s => s.Key == args.SidechainName);

            var defaultKeyValuePair = default(KeyValuePair<string, SidechainPool>);
            
            if (sidechainPoolValuePair.Equals(defaultKeyValuePair))
            {
                _logger.LogDebug($"Transaction received but sidechain {args.SidechainName} is unknown.");
                return;
            }

            var sidechainPool = sidechainPoolValuePair.Value;

            var sidechainSemaphore = TryGetAndAddSidechainSemaphore(sidechainPool.SmartContractAccount);

            await sidechainSemaphore.WaitAsync();

            try
            {
                if (!ValidationHelper.IsTransactionHashValid(transaction, out byte[] transactionHash))
                {
                    _logger.LogDebug($"Transaction hash not valid.");
                    return;
                }
                var databaseName = args.SidechainName;
                if (await _mongoDbProducerService.IsTransactionInDB(databaseName, transaction))
                {
                    _logger.LogDebug($"Already have transaction with same transaction hash or same sequence number.");
                    return;
                }
                var clientPublicKey = (await _mainChainService.RetrieveClientTable(args.SidechainName)).PublicKey;
                if (!SignatureHelper.VerifySignature(clientPublicKey, transaction.Signature, transactionHash))
                {
                    _logger.LogDebug($"Transaction signature not valid.");
                    return;
                }

                _logger.LogDebug($"Saving transaction.");
                await _mongoDbProducerService.SaveTransaction(databaseName, transaction);
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