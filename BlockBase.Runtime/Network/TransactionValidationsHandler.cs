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
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlockBase.Network.IO;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using BlockBase.Network.IO.Enums;
using BlockBase.Utils.Threading;
using BlockBase.Runtime.Helpers;

namespace BlockBase.Runtime.Network
{
    public class TransactionValidationsHandler
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private SidechainKeeper _sidechainKeeper;
        private IMongoDbProducerService _mongoDbProducerService;
        private IMainchainService _mainChainService;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ConcurrentDictionary<string, SemaphoreSlim> _validatorSemaphores;

        public TransactionValidationsHandler(ILogger<TransactionValidationsHandler> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, INetworkService networkService, SidechainKeeper sidechainKeeper, IMongoDbProducerService mongoDbProducerService, IMainchainService mainChainService)
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

            var receivedValidTransactions = new List<ulong>();
            _logger.LogDebug($"Received transaction #{transactionsProto.FirstOrDefault()?.SequenceNumber} to #{transactionsProto.LastOrDefault()?.SequenceNumber}");

            foreach (var transactionProto in transactionsProto)
            {
                if(receivedValidTransactions.Contains(transactionProto.SequenceNumber)) 
                    continue;

                var sequenceNumbers = await ValidateEachTransaction(transactionProto, args.ClientAccountName, sender);
                foreach (var sequenceNumber in sequenceNumbers)
                    if (sequenceNumber != 0 && !receivedValidTransactions.Contains(sequenceNumber))
                        receivedValidTransactions.Add(sequenceNumber);
            }

            var data = new List<byte>();
            foreach (var transactionSequenceNumber in receivedValidTransactions)
                data.AddRange(BitConverter.GetBytes(transactionSequenceNumber));

            if (data.Count() == 0)
                return;

            var message = new NetworkMessage(
                    NetworkMessageTypeEnum.ConfirmTransactionReception,
                    data.ToArray(),
                    TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey,
                    _nodeConfigurations.ActivePublicKey,
                    _networkConfigurations.PublicIpAddress + ":" + _networkConfigurations.TcpPort,
                    _nodeConfigurations.AccountName, sender);

            _logger.LogDebug("Sending confirmation transaction.");
            await _networkService.SendMessageAsync(message);
        }

        private async Task<IList<ulong>> ValidateEachTransaction(TransactionProto transactionProto, string clientAccountName, IPEndPoint sender)
        {

            var confirmedSequenceNumbers = new List<ulong>();

            var transaction = new Transaction().SetValuesFromProto(transactionProto);
            //_logger.LogDebug($"TRANSACTION {transaction.SequenceNumber} RECEIVED");

            var sidechainPoolValuePair = _sidechainKeeper.Sidechains.FirstOrDefault(s => s.Key == clientAccountName);

            var defaultKeyValuePair = default(KeyValuePair<string, SidechainPool>);

            if (sidechainPoolValuePair.Equals(defaultKeyValuePair))
            {
                _logger.LogDebug($"Transaction received but sidechain {clientAccountName} is unknown.");
                return confirmedSequenceNumbers;
            }

            var sidechainPool = sidechainPoolValuePair.Value;

            var sidechainSemaphore = TryGetAndAddSidechainSemaphore(sidechainPool.ClientAccountName);

            await sidechainSemaphore.WaitAsync();

            try
            {
                if (!ValidationHelper.IsTransactionHashValid(transaction, out byte[] transactionHash))
                {
                    _logger.LogDebug($"Transaction #{transaction.SequenceNumber} hash not valid.");
                    return confirmedSequenceNumbers;
                }
                var databaseName = clientAccountName;
                if (await _mongoDbProducerService.IsTransactionInDB(databaseName, transaction))
                {
                    //_logger.LogDebug($"Already have transaction with same transaction hash or same sequence number.");
                    var afterTransactions = await _mongoDbProducerService.GetTransactionsSinceSequenceNumber(_nodeConfigurations.AccountName, transaction.SequenceNumber);
                    confirmedSequenceNumbers.Add(transaction.SequenceNumber);
                    confirmedSequenceNumbers.AddRange(afterTransactions.Select(t => t.SequenceNumber));
                    return confirmedSequenceNumbers;
                }

                if (!SignatureHelper.VerifySignature(sidechainPool.ClientPublicKey, transaction.Signature, transactionHash))
                {
                    _logger.LogDebug($"Transaction signature not valid.");
                    return confirmedSequenceNumbers;
                }

                //_logger.LogDebug($"Saving transaction.");

                await _mongoDbProducerService.SaveTransaction(databaseName, transaction);

                confirmedSequenceNumbers.Add(transaction.SequenceNumber); 
            }
            catch (Exception e)
            {
                _logger.LogError($"Transaction validator crashed with exception {e.Message}");
            }
            finally
            {
                sidechainSemaphore.Release();
            }
            return confirmedSequenceNumbers;

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
        private void AddTransactionSequenceNumber()
        { }
        #endregion
    }
}