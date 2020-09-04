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
using BlockBase.DataPersistence.Data;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlockBase.Network.IO;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using BlockBase.Network.IO.Enums;
using BlockBase.Utils.Threading;
using BlockBase.Runtime.Helpers;
using Google.Protobuf;
using BlockBase.Runtime.Provider;
using static BlockBase.Network.PeerConnection;
using System.Text;

namespace BlockBase.Runtime.Network
{
    public class TransactionValidationsHandler
    {
        private INetworkService _networkService;
        private ILogger _logger;
        private SidechainKeeper _sidechainKeeper;
        private IMongoDbProducerService _mongoDbProducerService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private IMainchainService _mainChainService;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ConcurrentDictionary<string, SemaphoreSlim> _validatorSemaphores;

        public TransactionValidationsHandler(PeerConnectionsHandler peerConnectionsHandler, ILogger<TransactionValidationsHandler> logger, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, INetworkService networkService, SidechainKeeper sidechainKeeper, IMongoDbProducerService mongoDbProducerService, IMainchainService mainChainService)
        {
            _logger = logger;
            _logger.LogDebug("Creating transaction validator.");
            _networkService = networkService;
            _networkService.SubscribeTransactionsReceivedEvent(MessageForwarder_TransactionsReceived);
            _sidechainKeeper = sidechainKeeper;
            _mongoDbProducerService = mongoDbProducerService;
            _mainChainService = mainChainService;
            _nodeConfigurations = nodeConfigurations.Value;
            _validatorSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _networkConfigurations = networkConfigurations.Value;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        private async void MessageForwarder_TransactionsReceived(MessageForwarder.TransactionsReceivedEventArgs args, IPEndPoint sender)
        {
            try
            {
                _logger.LogDebug($"Receiving transaction for sidechain: {args.ClientAccountName}");
                var transactionsProto = SerializationHelper.DeserializeTransactions(args.TransactionsBytes, _logger);

                if (!_sidechainKeeper.TryGet(args.ClientAccountName, out var sidechainContext))
                {
                    _logger.LogDebug($"Transaction received but sidechain {args.ClientAccountName} is unknown.");
                    return;
                }

                if (transactionsProto == null) return;

                var receivedValidTransactions = new List<ulong>();
                var containsUnsavedTransactions = false;
                _logger.LogInformation($"Received transaction #{transactionsProto.FirstOrDefault()?.SequenceNumber} to #{transactionsProto.LastOrDefault()?.SequenceNumber} for sidechain {args.ClientAccountName}");

                foreach (var transactionProto in transactionsProto)
                {
                    if (receivedValidTransactions.Contains(transactionProto.SequenceNumber))
                        continue;

                    var transaction = new Transaction().SetValuesFromProto(transactionProto);
                    if (await ValidateTransaction(transaction, args.ClientAccountName, sidechainContext))
                    {
                        var isTransactionAlreadySaved = await CheckIfAlreadySavedTransactionAndSave(args.ClientAccountName, transaction);
                        if (!isTransactionAlreadySaved && !containsUnsavedTransactions) containsUnsavedTransactions = true;
                        receivedValidTransactions.Add(transaction.SequenceNumber);
                    }
                }

                if (!receivedValidTransactions.Any()) return;

                var lastTransaction = new Transaction().SetValuesFromProto(transactionsProto.Last());
                var alreadyReceivedTrxAfterLast = await GetConfirmedTransactionsSequeceNumber(lastTransaction, args.ClientAccountName);
                if (alreadyReceivedTrxAfterLast.Count > 0)
                    receivedValidTransactions.AddRange(alreadyReceivedTrxAfterLast);

                var data = new List<byte>();
                foreach (var transactionSequenceNumber in receivedValidTransactions)
                    data.AddRange(BitConverter.GetBytes(transactionSequenceNumber));

                var requesterPeer = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable().Where(p => p.ConnectionAccountName == args.ClientAccountName).FirstOrDefault();
                if (requesterPeer?.IPEndPoint?.Address.ToString() == sender.Address.ToString() && requesterPeer?.IPEndPoint?.Port == sender.Port)
                {
                    var message = new NetworkMessage(
                        NetworkMessageTypeEnum.ConfirmTransactionReception,
                        data.ToArray(),
                        TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey,
                        _nodeConfigurations.ActivePublicKey,
                        _networkConfigurations.GetResolvedIp() + ":" + _networkConfigurations.TcpPort,
                        _nodeConfigurations.AccountName, sender);
                    _logger.LogDebug("Sending confirmation transaction.");
                    await _networkService.SendMessageAsync(message);
                }

                //TODO: Temporarily remove logic, to replace with asking for transactions
                // if (containsUnsavedTransactions)
                // {
                //     await SendTransactionsToConnectedProviders(transactionsProto, args.ClientAccountName, sender, sidechainContext);
                // }
            }
            catch (Exception e)
            {
                _logger.LogError("Error handling received transactions");
                _logger.LogDebug($"Exception: {e}");
            }
        }

        private async Task SendTransactionsToConnectedProviders(IEnumerable<TransactionProto> transactions, string clientAccountName, IPEndPoint sender, SidechainContext sidechainContext)
        {
            var data = new List<byte>();
            var sidechainNameBytes = Encoding.UTF8.GetBytes(clientAccountName);
            short lenght = (short)sidechainNameBytes.Length;
            data.AddRange(BitConverter.GetBytes(lenght));
            data.AddRange(sidechainNameBytes);
            foreach (var transaction in transactions)
            {
                var transactionBytes = transaction.ToByteArray();
                data.AddRange(BitConverter.GetBytes(transactionBytes.Count()));
                data.AddRange(transactionBytes);
            }

            var sendTransactionTasks = new List<Task>();
            foreach (var producer in sidechainContext.SidechainPool.ProducersInPool.GetEnumerable().Where(p => p.PeerConnection != null && p.PeerConnection.ConnectionState == ConnectionStateEnum.Connected && p.PeerConnection.IPEndPoint != null))
            {
                if (producer.PeerConnection.IPEndPoint.Address.ToString() == sender.Address.ToString() && producer.PeerConnection.IPEndPoint.Port == sender.Port) continue;
                var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransactions, data.ToArray(), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.GetResolvedIp() + ":" + _networkConfigurations.TcpPort, _nodeConfigurations.AccountName, producer.PeerConnection.IPEndPoint);

                _logger.LogDebug($"Sending transactions #{transactions?.First()?.SequenceNumber} to #{transactions?.Last()?.SequenceNumber} to producer {producer.PeerConnection.ConnectionAccountName}");
                sendTransactionTasks.Add(_networkService.SendMessageAsync(message));
            }
            await Task.WhenAll(sendTransactionTasks);
        }

        public async Task<bool> ValidateTransaction(Transaction transaction, string clientAccountName, SidechainContext sidechainContext)
        {
            if (!ValidationHelper.IsTransactionHashValid(transaction, out byte[] transactionHash))
            {
                _logger.LogDebug($"Transaction #{transaction.SequenceNumber} hash not valid.");
                return false;
            }

            if (!SignatureHelper.VerifySignature(sidechainContext.SidechainPool.ClientPublicKey, transaction.Signature, transactionHash))
            {
                _logger.LogDebug($"Transaction signature not valid.");
                return false;
            }

            var transactionSize = transaction.ConvertToProto().ToByteArray().Count();

            if (transactionSize + BlockHeaderSizeConstants.BLOCKHEADER_MAX_SIZE > sidechainContext.SidechainPool.BlockSizeInBytes)
            {
                _logger.LogDebug($"Transaction is too big.");
                return false;
            }

            return true;
        }

        public async Task<bool> CheckIfAlreadySavedTransactionAndSave(string clientAccountName, Transaction transaction)
        {
            var alreadySaved = false;
            var sidechainSemaphore = TryGetAndAddSidechainSemaphore(clientAccountName);

            await sidechainSemaphore.WaitAsync();
            try
            {
                var databaseName = clientAccountName;
                if (!await _mongoDbProducerService.IsTransactionInDB(databaseName, transaction))
                {
                    await _mongoDbProducerService.SaveTransaction(databaseName, transaction);
                }
                else
                {
                    alreadySaved = true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Saving transaction crashed {e.Message}");
            }
            finally
            {
                sidechainSemaphore.Release();
            }

            return alreadySaved;
        }

        private async Task<IList<ulong>> GetConfirmedTransactionsSequeceNumber(Transaction transaction, string clientAccountName)
        {

            var confirmedSequenceNumbers = new List<ulong>();

            if (_sidechainKeeper.TryGet(clientAccountName, out var sidechainContext))
            {
                try
                {
                    var databaseName = clientAccountName;
                    if (await _mongoDbProducerService.IsTransactionInDB(databaseName, transaction))
                    {
                        //_logger.LogDebug($"Already have transaction with same transaction hash or same sequence number.");
                        var afterTransactions = await _mongoDbProducerService.GetTransactionsSinceSequenceNumber(_nodeConfigurations.AccountName, transaction.SequenceNumber);
                        confirmedSequenceNumbers.Add(transaction.SequenceNumber);
                        confirmedSequenceNumbers.AddRange(afterTransactions.Select(t => t.SequenceNumber));
                        return confirmedSequenceNumbers;
                    }

                    confirmedSequenceNumbers.Add(transaction.SequenceNumber);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Transaction validator crashed with exception {e.Message}");
                }
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

        #endregion
    }
}