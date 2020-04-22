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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Sidechain
{
    public class SidechainManager : IThreadableComponent
    {
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
            _blockProductionManager = new BlockProductionManager(Sidechain, _nodeConfigurations, _logger, _networkService, _peerConnectionsHandler, _mainchainService, _mongoDbProducerService, EndPoint, _blockSender, new SidechainDatabasesManager(connector));

            _mongoDbProducerService.CreateDatabasesAndIndexes(sidechain.ClientAccountName);
        }

        public async Task SuperMethod()
        {
            await Task.Delay(1000);
            try
            {
                while (true)
                {
                    try
                    {
                        switch (Sidechain.State)
                        {
                            case SidechainPoolStateEnum.WaitForNextState:
                                _timeDiff = (Sidechain.NextStateWaitEndTime * 1000) - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                if (_timeDiff <= 0)
                                {
                                    await CheckContractAndUpdateStates();
                                    await CheckContractAndUpdateWaitTimes();
                                    await CheckContractEndState();
                                    if (Sidechain.ProducingBlocks && !Sidechain.CandidatureOnStandby && _previousWaitTime != Sidechain.NextStateWaitEndTime)
                                    {
                                        await CheckPeerConnections();
                                        await CheckContractAndUpdatePool();
                                        await CheckAndGetReward();
                                        await CheckSidechainValidation();
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
                    catch (ApiErrorException e)
                    {
                        _logger.LogCritical($"Failed to send transaction: {e.Message}");
                    }
                    catch (ApiException e)
                    {
                        _logger.LogCritical($"Failed to communicate with EOS endpoint: {e.Message}");
                    }
                    catch (HttpRequestException e)
                    {
                        _logger.LogCritical($"Failed to communicate with EOS endpoint: {e.Message}");
                    }
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
            Sidechain.ClientPublicKey = (await _mainchainService.RetrieveClientTable(Sidechain.ClientAccountName)).PublicKey;

            _logger.LogDebug("Recover Info");

            var contractInformation = await _mainchainService.RetrieveContractInformation(Sidechain.ClientAccountName);

            Sidechain.BlockTimeDuration = contractInformation.BlockTimeDuration;
            Sidechain.BlocksBetweenSettlement = contractInformation.BlocksBetweenSettlement;
            Sidechain.BlockSizeInBytes = contractInformation.SizeOfBlockInBytes;

            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);

            if (IsProducerInTable(producersInTable))
            {
                var self = producersInTable.Where(p => p.Key == _nodeConfigurations.AccountName).FirstOrDefault();
                Sidechain.ProducerType = (ProducerTypeEnum)self.ProducerType;

                var producersInPool = producersInTable.Select(m => new ProducerInPool
                {
                    ProducerInfo = new ProducerInfo
                    {
                        AccountName = m.Key,
                        PublicKey = m.PublicKey,
                        ProducerType = (ProducerTypeEnum)m.ProducerType,
                        NewlyJoined = true
                    }
                }).ToList();

                Sidechain.ProducersInPool.ClearAndAddRange(producersInPool);

                if (Sidechain.ProducingBlocks) await InitProducerReceiveIPs();
            }
            else
            {
                var candidatesInTable = await _mainchainService.RetrieveCandidates(Sidechain.ClientAccountName);
                var self = candidatesInTable.Where(p => p.Key == _nodeConfigurations.AccountName).FirstOrDefault();
                Sidechain.ProducerType = (ProducerTypeEnum)self.ProducerType;
                Sidechain.CandidatureOnStandby = true;
            }

            _logger.LogDebug("State " + Sidechain.State);
        }

        private async Task InitCandidature()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            if (Sidechain.ProducingBlocks && !Sidechain.CandidatureOnStandby) return;

            _logger.LogInformation("Init candidature.");
            var contractInformation = await _mainchainService.RetrieveContractInformation(Sidechain.ClientAccountName);
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
            var contractInformation = await _mainchainService.RetrieveContractInformation(Sidechain.ClientAccountName);
            var contractState = await _mainchainService.RetrieveContractState(Sidechain.ClientAccountName);

            var secretPass = _nodeConfigurations.SecretPassword;
            _logger.LogDebug("Secret sent: " + _nodeConfigurations.SecretPassword);
            var secret = HashHelper.Sha256Data(Encoding.ASCII.GetBytes(secretPass));

            _logger.LogDebug($"Sending secret: {HashHelper.ByteArrayToFormattedHexaString(secret)}");

            await _mainchainService.AddSecret(Sidechain.ClientAccountName, _nodeConfigurations.AccountName, HashHelper.ByteArrayToFormattedHexaString(secret));
        }

        private async Task CheckCandidatureSuccess()
        {
            Sidechain.State = SidechainPoolStateEnum.WaitForNextState;

            _logger.LogInformation("End Candidature.");

            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);

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
                            ProducerType = (ProducerTypeEnum)m.ProducerType,
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
            await EncryptAndSendIPToSmartContract(Sidechain.ClientPublicKey, Sidechain.ClientAccountName);
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

            await _mainchainService.NotifyReady(Sidechain.ClientAccountName, _nodeConfigurations.AccountName);
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
                    _peerConnectionsHandler.TryToRemoveConnection(leavingProducer.PeerConnection);
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
                            ProducerType = (ProducerTypeEnum)producersInTable[i].ProducerType,
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
            var ipAddresses = await _mainchainService.RetrieveIPAddresses(Sidechain.ClientAccountName);

            UpdateIPsInSidechain(ipAddresses);

            if (!(Sidechain.ProducersInPool.GetEnumerable().Count() == 1))
            {
                await _peerConnectionsHandler.UpdateConnectedProducersInSidechainPool(Sidechain);
            }
        }

        private async Task EncryptAndSendIPToSmartContract(string clientPublicKey, string SidechainName)
        {
            int numberOfIpsToSend = (int)Math.Ceiling(Sidechain.ProducersInPool.Count() / 4.0);
            var producerList = Sidechain.ProducersInPool.GetEnumerable().ToList();
            var keysToUse = ListHelper.GetListSortedCountingFrontFromIndex(producerList, producerList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)).Take(numberOfIpsToSend).Select(p => p.ProducerInfo.PublicKey).ToList();
            keysToUse.Add(clientPublicKey);

            _logger.LogDebug($"Sending {keysToUse.Count} ips.");

            var listEncryptedIps = new List<string>();
            foreach (string receiverPublicKey in keysToUse)
            {
                _logger.LogDebug("Key to use: " + receiverPublicKey);
                listEncryptedIps.Add(IPEncryption.EncryptIP(EndPoint, _nodeConfigurations.ActivePrivateKey, receiverPublicKey));
            }

            await _mainchainService.AddEncryptedIps(SidechainName, _nodeConfigurations.AccountName, listEncryptedIps);
        }

        private void UpdateIPsInSidechain(List<IPAddressTable> IpsAddressTableEntries)
        {
            if (!IpsAddressTableEntries.Any() || IpsAddressTableEntries.Any(t => !t.EncryptedIPs.Any())) return;
            foreach (var ipAddressTable in IpsAddressTableEntries) ipAddressTable.EncryptedIPs.RemoveAt(ipAddressTable.EncryptedIPs.Count - 1);

            int numberOfIpsToUpdate = (int)Math.Ceiling(Sidechain.ProducersInPool.Count() / 4.0);
            if (numberOfIpsToUpdate == 0) return;

            var producersInPoolList = Sidechain.ProducersInPool.GetEnumerable().ToList();
            if (!producersInPoolList.Any(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)) return;
            var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)).Take(numberOfIpsToUpdate).ToList();

            foreach (var producer in orderedProducersInPool)
            {
                var producerIndex = orderedProducersInPool.IndexOf(producer);
                var producerIps = IpsAddressTableEntries.Where(p => p.Key == producer.ProducerInfo.AccountName).FirstOrDefault();
                if (producerIps == null || producer.ProducerInfo.IPEndPoint != null) continue;

                var listEncryptedIPEndPoints = producerIps.EncryptedIPs;
                var encryptedIpEndPoint = listEncryptedIPEndPoints[producerIndex];
                producer.ProducerInfo.IPEndPoint = IPEncryption.DecryptIP(encryptedIpEndPoint, _nodeConfigurations.ActivePrivateKey, producer.ProducerInfo.PublicKey);
            }
        }

        private async Task CheckContractAndUpdateStates()
        {
            var contractState = await _mainchainService.RetrieveContractState(Sidechain.ClientAccountName);
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
            var contractInfo = await _mainchainService.RetrieveContractInformation(Sidechain.ClientAccountName);
            var currentProd = (await _mainchainService.RetrieveCurrentProducer(Sidechain.ClientAccountName)).SingleOrDefault();
            if (Sidechain.State == SidechainPoolStateEnum.ConfigTime) Sidechain.NextStateWaitEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (contractInfo.CandidatureTime / 2);
            if (Sidechain.State == SidechainPoolStateEnum.CandidatureTime && contractInfo.CandidatureEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.CandidatureEndDate;
            if (Sidechain.State == SidechainPoolStateEnum.SecretTime && contractInfo.SecretEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.SecretEndDate;
            if (Sidechain.State == SidechainPoolStateEnum.IPSendTime && contractInfo.SendEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.SendEndDate;
            if (Sidechain.State == SidechainPoolStateEnum.IPReceiveTime && contractInfo.ReceiveEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) Sidechain.NextStateWaitEndTime = contractInfo.ReceiveEndDate;

            if (!Sidechain.ProducingBlocks || Sidechain.CandidatureOnStandby) return;

            var nextBlockTime = currentProd != null ?
                currentProd.StartProductionTime + Sidechain.BlockTimeDuration :
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Sidechain.BlockTimeDuration;
            if (nextBlockTime < Sidechain.NextStateWaitEndTime || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= Sidechain.NextStateWaitEndTime)
                Sidechain.NextStateWaitEndTime = nextBlockTime;
        }

        private async Task CheckContractEndState()
        {
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);
            var candidatesInTable = await _mainchainService.RetrieveCandidates(Sidechain.ClientAccountName);

            if (Sidechain.State == SidechainPoolStateEnum.ConfigTime || (!IsProducerInTable(producersInTable) && !candidatesInTable.Select(m => m.Key).Contains(_nodeConfigurations.AccountName)))
            {
                _logger.LogInformation("Smart Contract Ended");

                _peerConnectionsHandler.RemovePoolConnections(Sidechain);
                _blockProductionManager?.TaskContainer?.Stop();
                // await _mongoDbProducerService.RemoveSidechainFromDatabaseAsync(Sidechain.SidechainName); //TODO: Check this again, probably not necessary to delete this automatically, add endpoint to delete manually

                TaskContainer.Stop();
            }
        }

        private async Task CheckPeerConnections()
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);
            var producersInPool = producersInTable.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    NewlyJoined = false,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    IPEndPoint = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()?.IPEndPoint
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            Sidechain.ProducersInPool.ClearAndAddRange(producersInPool);

            var ipAddresses = await _mainchainService.RetrieveIPAddresses(Sidechain.ClientAccountName);
            UpdateIPsInSidechain(ipAddresses);

            if (Sidechain.ProducersInPool.GetEnumerable().Any(p => p.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected))
            {
                var checkConnectionTask = TaskContainer.Create(async () => await _peerConnectionsHandler.CheckConnectionStatus(Sidechain));
                checkConnectionTask.Start();
            }
        }

        private async Task CheckContractAndUpdatePool()
        {
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);
            if (producersInTable == null || !producersInTable.Any() || !IsProducerInTable(producersInTable)) return;

            await _peerConnectionsHandler.UpdateConnectedProducersInSidechainPool(Sidechain);
        }

        private async Task CheckAndGetReward()
        {
            var rewardTable = await _mainchainService.RetrieveRewardTable(_nodeConfigurations.AccountName);
            if (rewardTable.Any(r => r.Reward > 0 && r.Key == Sidechain.ClientAccountName))
            {
                await HistoryValidationHelper.CheckSidechainValidationProposal(_mainchainService, _nodeConfigurations.AccountName, Sidechain.ClientAccountName, _logger);
                await _mainchainService.ClaimReward(Sidechain.ClientAccountName, _nodeConfigurations.AccountName);
            }
        }

        private async Task CheckSidechainValidation()
        {
            var sidechainValidation = await _mainchainService.RetrieveHistoryValidationTable(Sidechain.ClientAccountName);
            if (sidechainValidation == null) return;

            if (sidechainValidation.Key == _nodeConfigurations.AccountName)
            {
                await HistoryValidationHelper.ProposeHistoryValidationAndTryToExecute(
                _mainchainService,
                _mongoDbProducerService,
                _nodeConfigurations.AccountName,
                sidechainValidation.BlockHash,
                Sidechain,
                _logger);
            }
            else
            {
                await HistoryValidationHelper.CheckAndApproveHistoryValidation(_mainchainService, _mongoDbProducerService, _nodeConfigurations.AccountName, Sidechain.ClientAccountName, _logger);
            }
        }

        #endregion Auxiliar Methods
    }
}