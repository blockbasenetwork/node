using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Runtime.Sidechain
{
    public class SidechainManager : IThreadableComponent
    {
        //TODO: marciak - build chain method.
        //encrypt and decrypt IPs methods

        //rpinto - this isn't thread safe. Anything that may be accessed from a different running thread must be thread safe.  -> done - marciak
        //private SidechainKeeper _sidechainKeeper;
        public SidechainPool Sidechain { get; set; }

        public TaskContainer TaskContainer { get; private set; }
        public string EndPoint { get; set; }

        private BlockProductionManager _blockProductionManager;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private readonly INetworkService _networkService;
        private IMainchainService _mainchainService;
        private long _timeDiff;
        private double _previousWaitTime;
        private readonly NetworkConfigurations _networkConfigurations;
        private readonly NodeConfigurations _nodeConfigurations;
        private ILogger _logger;
        private readonly IMongoDbProducerService _mongoDbProducerService;
        private readonly BlockSender _blockSender;

        private const uint MINIMUM_NUMBER_OF_CONNECTIONS = 1;
        private const uint CONNECTION_EXPIRATION_TIME_IN_SECONDS_MAINCHAIN = 60;
        private const int MAX_NUMBER_OF_TRIES = 5;
        private const int SETTLEMENT_BLOCKS_PER_PRODUCER = 5;

        public TaskContainer Start()
        {
            TaskContainer = TaskContainer.Create(async () => await SuperMethod());
            TaskContainer.Start();
            return TaskContainer;
        }

        public SidechainManager(SidechainPool sidechain, PeerConnectionsHandler peerConnectionsHandler, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, string endpoint, ILogger logger, INetworkService networkService, IMongoDbProducerService mongoDbProducerService, BlockSender blockSender, IMainchainService mainchainService)
        {
            Sidechain = sidechain;
            EndPoint = endpoint;
            _networkService = networkService;
            _mainchainService = mainchainService;
            _logger = logger;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbProducerService = mongoDbProducerService;
            _blockSender = blockSender;
            //TODO - commented this code in order to be able to remove completely the old Query Builder and its direct dependencies
            //TODO - it should receive an IConnector passed through dependency injection
            IConnector connector = null; // new MySqlConnector(_nodeConfigurations.MySqlServer, _nodeConfigurations.MySqlUser, _nodeConfigurations.MySqlPort, _nodeConfigurations.MySqlPassword, logger);
            _blockProductionManager = new BlockProductionManager(Sidechain, _nodeConfigurations, _logger, _networkService, _mainchainService, _mongoDbProducerService, EndPoint, _blockSender, new SidechainDatabasesManager(connector));
        }

        public async Task SuperMethod()
        {
            try
            {
                while (true)
                {
                    switch (Sidechain.State)
                    {
                        case SidechainPoolStateEnum.WaitForNextState:
                            _timeDiff = (Sidechain.NextStateWaitEndTime * 1000) - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            if (_timeDiff <= 0)
                            {
                                await CheckContractAndUpdateStates();
                                await CheckContractAndUpdateWaitTimes();
                                if (Sidechain.ProducingBlocks)
                                {
                                    await CheckContractEndState();
                                    await CheckContractAndUpdatePool();
                                };
                            }
                            else await Task.Delay((int)_timeDiff);

                            if (_previousWaitTime == Sidechain.NextStateWaitEndTime)
                            {
                                await Task.Delay(50);
                                Sidechain.State = SidechainPoolStateEnum.WaitForNextState;
                            }
                            break;

                        case SidechainPoolStateEnum.CandidatureTime:
                            await InitCandidature();
                            break;

                        case SidechainPoolStateEnum.SecretTime:
                            await SendSecret();
                            break;

                        case SidechainPoolStateEnum.IPSendTime:
                            await CheckCandidatureSuccess();
                            await InitProducerSendIP();
                            break;

                        case SidechainPoolStateEnum.IPReceiveTime:
                            await InitProducerReceiveIPs();
                            break;

                        case SidechainPoolStateEnum.InitMining:
                            await InitMining();
                            break;

                        case SidechainPoolStateEnum.RecoverInfo:
                            await RecoverInfo();
                            break;
                    }
                    TaskContainer.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Sidechain Service stopped.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Sidechain Manager crashed. Exception: {ex}");
            }
        }

        #region Switch Methods

        private async Task RecoverInfo()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            _logger.LogDebug("Recover Info");

            var contractInformation = await _mainchainService.RetrieveContractInformation(Sidechain.SidechainName);

            Sidechain.BlockTimeDuration = contractInformation.BlockTimeDuration;
            Sidechain.BlocksBetweenSettlement = contractInformation.BlocksBetweenSettlement;

            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.SidechainName);

            if (IsProducerInTable(producersInTable))
            {
                var producersInPool = producersInTable.Select(m => new ProducerInPool
                {
                    ProducerInfo = new ProducerInfo
                    {
                        AccountName = m.Key,
                        PublicKey = m.PublicKey,
                        NewlyJoined = true
                    }
                }).ToList();

                Sidechain.ProducersInPool.ClearAndAddRange(producersInPool);
            }

            if (Sidechain.ProducingBlocks) await InitProducerReceiveIPs();

            _logger.LogDebug("State " + Sidechain.State);
        }

        private async Task InitCandidature()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            if (Sidechain.ProducingBlocks && !Sidechain.CandidatureOnStandby) return;

            _logger.LogInformation("Init candidature.");
            var contractInformation = await _mainchainService.RetrieveContractInformation(Sidechain.SidechainName);
            if (contractInformation == null) _logger.LogCritical("contract info null");
            if (Sidechain == null) _logger.LogCritical("contract info null");
            Sidechain.BlockTimeDuration = contractInformation.BlockTimeDuration;
            Sidechain.BlocksBetweenSettlement = contractInformation.BlocksBetweenSettlement;
            _logger.LogDebug($"Block time duration: {Sidechain.BlockTimeDuration} seconds, Settlement Blocks: {Sidechain.BlocksBetweenSettlement} blocks");
        }

        private async Task SendSecret()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            if (Sidechain.ProducingBlocks && !Sidechain.CandidatureOnStandby) return;

            _logger.LogInformation("Sending Secret...");
            var contractInformation = await _mainchainService.RetrieveContractInformation(Sidechain.SidechainName);
            var contractState = await _mainchainService.RetrieveContractState(Sidechain.SidechainName);

            var secretPass = _nodeConfigurations.SecretPassword;
            _logger.LogDebug("Secret sent: " + _nodeConfigurations.SecretPassword);
            var secret = HashHelper.Sha256Data(Encoding.ASCII.GetBytes(secretPass));

            _logger.LogDebug($"Sending secret: {HashHelper.ByteArrayToFormattedHexaString(secret)}");

            try
            {
                await _mainchainService.AddSecret(Sidechain.SidechainName, _nodeConfigurations.AccountName, HashHelper.ByteArrayToFormattedHexaString(secret));
            }
            catch (ApiErrorException e)
            {
                _logger.LogCritical($"Failed to send secret: {e.Message}");
            }
        }

        private async Task CheckCandidatureSuccess()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            _logger.LogInformation("End Candidature.");

            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.SidechainName);

            if (IsProducerInTable(producersInTable))
            {
                if (!Sidechain.ProducersInPool.GetEnumerable().Any())
                {
                    var producersInPool = producersInTable.Select(m => new ProducerInPool
                    {
                        ProducerInfo = new ProducerInfo
                        {
                            AccountName = m.Key,
                            PublicKey = m.PublicKey,
                            NewlyJoined = true
                        }
                    }).ToList();

                    Sidechain.ProducersInPool.ClearAndAddRange(producersInPool);
                }
                else
                {
                    UpdateAndCheckIfProducersInSidechainChanged(producersInTable);
                }
                Sidechain.CandidatureOnStandby = false;
            }
            else
            {
                _logger.LogInformation("Didn't enter producer selection for sidechain. Candidature on standby...");
                Sidechain.CandidatureOnStandby = true;
            }
        }

        private async Task InitProducerSendIP()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            if (Sidechain.CandidatureOnStandby) return;

            _logger.LogInformation("Init producer send IP.");
            var producersPublicKeys = Sidechain.ProducersInPool.GetEnumerable().Select(p => p.ProducerInfo.PublicKey).ToList();
            var clientPublicKey = (await _mainchainService.RetrieveClientTable(Sidechain.SidechainName)).PublicKey;
            await EncryptAndSendIPToSmartContract(producersPublicKeys, clientPublicKey, Sidechain.SidechainName);
        }

        private async Task InitProducerReceiveIPs()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            if (Sidechain.CandidatureOnStandby) return;

            _logger.LogInformation("Init producer receive ips.");
            await ExtractAndUpdateIPs();

            //TODO: Review this code
            if (Sidechain.ProducingBlocks && !Sidechain.CandidatureOnStandby && _blockProductionManager.TaskContainer == null)
            {
                _blockProductionManager.Start();
                return;
            }

            if (_blockProductionManager.TaskContainer == null) await ReadyToProduce();
        }

        private async Task InitMining()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            if (Sidechain.CandidatureOnStandby) return;
            if (Sidechain.ProducersInPool.GetEnumerable().Count(m => m.ProducerInfo.IPEndPoint != null) == 0) await ExtractAndUpdateIPs();

            _logger.LogDebug("Starting block production manager.");
            _blockProductionManager.Start();
        }

        #endregion Switch Methods

        #region Auxiliar Methods

        private async Task ReadyToProduce()
        {
            _logger.LogDebug("Sending transaction to confirm it is ready do produce.");

            try
            {
                await _mainchainService.NotifyReady(Sidechain.SidechainName, _nodeConfigurations.AccountName);
            }
            catch (ApiErrorException e)
            {
                _logger.LogCritical($"Failed to send notify ready: {e.Message}");
            }
        }

        private bool UpdateAndCheckIfProducersInSidechainChanged(List<ProducerInTable> producersInTable)
        {
            var changed = RemoveLeavingProducersFromPool(producersInTable);
            var changed2 = AddNewProducerToPool(producersInTable);

            return changed || changed2;
        }

        public bool RemoveLeavingProducersFromPool(List<ProducerInTable> producersInTable)
        {
            var accountNamesProducersInPool = Sidechain.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).ToList();
            var accountNamesInTable = producersInTable.Select(m => m.Key).ToList();

            var poolChanged = false;

            for (int i = 0; i < Sidechain.ProducersInPool.GetEnumerable().Count(); i++)
            {
                if (!accountNamesInTable.Contains(accountNamesProducersInPool[i]))
                {
                    _logger.LogInformation("Some producer Left.");
                    var leavingProducer = Sidechain.ProducersInPool.Get(i);
                    _peerConnectionsHandler.RemoveProducerConnectionIfPossible(leavingProducer);
                    Sidechain.ProducersInPool.Remove(leavingProducer);
                    poolChanged = true;
                }
            }
            return poolChanged;
        }

        public bool AddNewProducerToPool(List<ProducerInTable> producersInTable)
        {
            var accountNamesProducersInPool = Sidechain.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.AccountName).ToList();
            var accountNamesInTable = producersInTable.Select(m => m.Key).ToList();

            var poolChanged = false;

            for (int i = 0; i < accountNamesInTable.Count(); i++)
            {
                if (!accountNamesProducersInPool.Contains(accountNamesInTable[i]))
                {
                    _logger.LogInformation("New producer joined.");
                    Sidechain.ProducersInPool.Insert(i, new ProducerInPool()
                    {
                        ProducerInfo = new ProducerInfo()
                        {
                            AccountName = producersInTable[i].Key,
                            IPEndPoint = null,
                            PublicKey = producersInTable[i].PublicKey,
                            NewlyJoined = true
                        }
                    });
                    poolChanged = true;
                }
            }
            return poolChanged;
        }

        private bool IsProducerInTable(List<ProducerInTable> producersInTable)
        {
            if (!producersInTable.Select(m => m.Key).Contains(_nodeConfigurations.AccountName))
            {
                return false;
            }
            return true;
        }

        private async Task ExtractAndUpdateIPs()
        {
            var ipAddresses = await _mainchainService.RetrieveIPAddresses(Sidechain.SidechainName);

            UpdateIPsInSidechain(ipAddresses);

            Sidechain.ProducersInPool.GetEnumerable().Select(m => m.ProducerInfo.NewlyJoined = false);
            if (!(Sidechain.ProducersInPool.GetEnumerable().Count() == 1 ||
               (Sidechain.ProducersInPool.GetEnumerable().Count() == 2 && Sidechain.ProducersInPool.GetEnumerable().First().ProducerInfo.PublicKey == _nodeConfigurations.ActivePublicKey)))
            {
                await _peerConnectionsHandler.UpdateConnectedProducersInSidechainPool(Sidechain);
            }
        }

        private async Task EncryptAndSendIPToSmartContract(List<string> producersPublicKeys, string clientPublicKey, string SidechainName)
        {
            int numberOfIpsToSend = (int)Math.Ceiling(Sidechain.ProducersInPool.Count() / 4.0);
            var keysToUse = ListHelper.GetListSortedCountingFrontFromIndex(producersPublicKeys, producersPublicKeys.IndexOf(_nodeConfigurations.ActivePublicKey)).Take(numberOfIpsToSend).ToList();
            keysToUse.Add(clientPublicKey);

            _logger.LogDebug($"Sending {keysToUse.Count} ips.");

            var listEncryptedIps = new List<string>();
            foreach (string receiverPublicKey in keysToUse)
            {
                _logger.LogDebug("Key to use: " + receiverPublicKey);
                listEncryptedIps.Add(EncryptIP(receiverPublicKey));
            }

            try
            {
                await _mainchainService.AddEncryptedIps(SidechainName, _nodeConfigurations.AccountName, listEncryptedIps);
            }
            catch (ApiErrorException e)
            {
                _logger.LogCritical($"Failed to send encrypted ips: {e.Message}");
            }
        }

        private string EncryptIP(string receiverPublicKey)
        {
            // _logger.LogDebug($"Receiver public key {receiverPublicKey}, Endpoint {EndPoint}");
            var ipBytes = Encoding.UTF8.GetBytes(EndPoint);
            // _logger.LogDebug($"endpoint bytes {HashHelper.ByteArrayToFormattedHexaString(ipBytes)}");
            var encryptedIP = AssymetricEncryptionHelper.EncryptData(receiverPublicKey, _nodeConfigurations.ActivePrivateKey, ipBytes);
            // _logger.LogDebug($"encryptedIP {HashHelper.ByteArrayToFormattedHexaString(encryptedIP)}");
            return HashHelper.ByteArrayToFormattedHexaString(encryptedIP);
        }

        private string DecryptIP(string encryptedIP, string senderPublicKey)
        {
            // _logger.LogDebug($"Sender public key {senderPublicKey}, Encrypted IP {EndPoint}");
            var encryptedIPBytes = HashHelper.FormattedHexaStringToByteArray(encryptedIP);
            // _logger.LogDebug($"encryptedIP {HashHelper.ByteArrayToFormattedHexaString(encryptedIPBytes)}");
            var ipBytes = AssymetricEncryptionHelper.DecryptData(senderPublicKey, _nodeConfigurations.ActivePrivateKey, encryptedIPBytes);
            // _logger.LogDebug($"endpoint bytes {HashHelper.ByteArrayToFormattedHexaString(ipBytes)}");
            _logger.LogDebug("Decrypted IP: " + Encoding.UTF8.GetString(ipBytes));

            return Encoding.UTF8.GetString(ipBytes);
        }

        private void UpdateIPsInSidechain(List<IPAddressTable> IpsAddressTableEntries)
        {
            foreach (var ipAddressTable in IpsAddressTableEntries) ipAddressTable.EncryptedIPs.RemoveAt(ipAddressTable.EncryptedIPs.Count - 1);

            var producerIndex = IpsAddressTableEntries.FindIndex(m => m.Key == _nodeConfigurations.AccountName);
            int numberOfIpsToUpdate = (int)Math.Ceiling(Sidechain.ProducersInPool.Count() / 4.0);
            _logger.LogDebug($"Updating {numberOfIpsToUpdate} ips.");
            var reorganizedIpsAddressTableEntries = ListHelper.GetListSortedCountingBackFromIndex(IpsAddressTableEntries, producerIndex).Take(numberOfIpsToUpdate).ToList();

            for (int i = 0; i < reorganizedIpsAddressTableEntries.Count(); i++)
            {
                var producer = Sidechain.ProducersInPool.GetEnumerable().Where(m => m.ProducerInfo.AccountName == reorganizedIpsAddressTableEntries[i].Key).SingleOrDefault();
                if (producer == null || producer.ProducerInfo.IPEndPoint != null) continue;

                var listEncryptedIPEndPoints = reorganizedIpsAddressTableEntries[i].EncryptedIPs;
                var encryptedIpEndPoint = listEncryptedIPEndPoints[i];
                _logger.LogDebug("Received IP from producer " + producer.ProducerInfo.AccountName + " with pk " + producer.ProducerInfo.PublicKey);
                var ipEndPoint = DecryptIP(encryptedIpEndPoint, producer.ProducerInfo.PublicKey);

                string[] splitIPEndPoint = ipEndPoint.Split(':');

                if (!IPAddress.TryParse(splitIPEndPoint[0], out var ipAddress)) continue;
                if (!int.TryParse(splitIPEndPoint[1], out var port)) continue;

                producer.ProducerInfo.IPEndPoint = new IPEndPoint(ipAddress, port);
            }
        }

        private async Task CheckContractAndUpdateStates()
        {
            var contractState = await _mainchainService.RetrieveContractState(Sidechain.SidechainName);
            if (contractState.ConfigTime) Sidechain.State = SidechainPoolStateEnum.ConfigTime;
            if (contractState.CandidatureTime) Sidechain.State = SidechainPoolStateEnum.CandidatureTime;
            if (contractState.SecretTime) Sidechain.State = SidechainPoolStateEnum.SecretTime;
            if (contractState.IPSendTime) Sidechain.State = SidechainPoolStateEnum.IPSendTime;
            if (contractState.IPReceiveTime) Sidechain.State = SidechainPoolStateEnum.IPReceiveTime;

            if (contractState.ProductionTime != Sidechain.ProducingBlocks)
            {
                if (contractState.ProductionTime) Sidechain.State = SidechainPoolStateEnum.InitMining;
                Sidechain.ProducingBlocks = contractState.ProductionTime;
            }
        }

        private async Task CheckContractAndUpdateWaitTimes()
        {
            _previousWaitTime = Sidechain.NextStateWaitEndTime;
            var contractInfo = await _mainchainService.RetrieveContractInformation(Sidechain.SidechainName);
            var lastBlockFromSettlement = await _mainchainService.RetrieveLastBlockFromLastSettlement(Sidechain.SidechainName);
            if (Sidechain.State == SidechainPoolStateEnum.ConfigTime) Sidechain.NextStateWaitEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (contractInfo.CandidatureTime / 2);
            if (Sidechain.State == SidechainPoolStateEnum.CandidatureTime && contractInfo.CandidatureEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.CandidatureEndDate;
            if (Sidechain.State == SidechainPoolStateEnum.SecretTime && contractInfo.SecretEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.SecretEndDate;
            if (Sidechain.State == SidechainPoolStateEnum.IPSendTime && contractInfo.SendEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.SendEndDate;
            if (Sidechain.State == SidechainPoolStateEnum.IPReceiveTime && contractInfo.ReceiveEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.ReceiveEndDate;

            if (!Sidechain.ProducingBlocks) return;

            var nextSettlementTime = lastBlockFromSettlement != null ?
                lastBlockFromSettlement.Timestamp + (Sidechain.BlockTimeDuration * (Sidechain.BlocksBetweenSettlement + 1)) :
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (Sidechain.BlockTimeDuration * (Sidechain.BlocksBetweenSettlement + 1));
            if (nextSettlementTime < Sidechain.NextStateWaitEndTime || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= Sidechain.NextStateWaitEndTime)
                Sidechain.NextStateWaitEndTime = nextSettlementTime;
        }

        private async Task CheckContractEndState()
        {
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.SidechainName);
            var candidatesInTable = await _mainchainService.RetrieveCandidates(Sidechain.SidechainName);

            if (Sidechain.State == SidechainPoolStateEnum.ConfigTime || (!IsProducerInTable(producersInTable) && !candidatesInTable.Select(m => m.Key).Contains(_nodeConfigurations.AccountName)))
            {
                _logger.LogInformation("Smart Contract Ended");

                _peerConnectionsHandler.RemovePoolConnections(Sidechain);
                _blockProductionManager?.TaskContainer?.Stop();
                // await _mongoDbProducerService.RemoveSidechainFromDatabaseAsync(Sidechain.SidechainName); //TODO: Check this again, probably not necessary to delete this automatically, add endpoint to delete manually

                TaskContainer.Stop();
            }
        }

        private async Task CheckContractAndUpdatePool()
        {
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.SidechainName);
            if (producersInTable == null || !producersInTable.Any() || !IsProducerInTable(producersInTable)) return;

            _logger.LogInformation("Checking if pool changed...");
            bool poolChanged = UpdateAndCheckIfProducersInSidechainChanged(producersInTable);
            await _peerConnectionsHandler.TryReconnectWithDisconnectedAccounts(Sidechain);
            if (poolChanged)
            {
                _logger.LogInformation("Pool changed.");
                await _peerConnectionsHandler.UpdateConnectedProducersInSidechainPool(Sidechain);
            }
        }

        #endregion Auxiliar Methods
    }
}