using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Eos;
using BlockBase.Domain.Enums;
using System.Collections.Generic;
using EosSharp.Core.Exceptions;
using BlockBase.Runtime.Network;
using System.Net;
using BlockBase.Utils.Crypto;
using BlockBase.Domain.Configurations;
using BlockBase.Domain;
using System.Net.Http;
using static BlockBase.Network.PeerConnection;
using BlockBase.Runtime.Sidechain;
using Microsoft.Extensions.Options;

namespace BlockBase.Runtime.Mainchain
{
    public class SidechainMaintainerManager
    {
        public SidechainPool _sidechain { get; set; }
        private IMainchainService _mainchainService;
        private long _timeDiff;
        private bool _forceTryAgain;
        private int _timeToExecuteTrx;
        private int _roundsUntilSettlement;
        private IEnumerable<int> _latestTrxTimes;
        private double _previousWaitTime;
        private ILogger _logger;
        private NodeConfigurations _nodeConfigurations;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private const float DELAY_IN_SECONDS = 0.5f;
        public TaskContainer TaskContainer { get; private set; }

        public TaskContainer Start()
        {
            TaskContainer = TaskContainer.Create(async () => await SuperMethod());
            TaskContainer.Start();
            return TaskContainer;
        }
        public SidechainMaintainerManager(ILogger<SidechainMaintainerManager> logger, IMainchainService mainchainService, IOptions<NodeConfigurations> nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler)
        {
            _peerConnectionsHandler = peerConnectionsHandler;
            _mainchainService = mainchainService;
            _roundsUntilSettlement = 0;
            _logger = logger;
            _latestTrxTimes = new List<int>();
            _nodeConfigurations = nodeConfigurations.Value;
            _sidechain = new SidechainPool(_nodeConfigurations.AccountName);
            _forceTryAgain = true;
        }

        public async Task SuperMethod()
        {
            try
            {
                var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.ClientAccountName);
                var blocksCount = await _mainchainService.RetrieveBlockCount(_sidechain.ClientAccountName);
                var numberOfRoundsAlreadyPassed = blocksCount.Sum(b => b.blocksproduced) + blocksCount.Sum(b => b.blocksfailed);

                _sidechain.BlockSizeInBytes = contractInfo.SizeOfBlockInBytes;
                _sidechain.BlockTimeDuration = contractInfo.BlockTimeDuration;
                _sidechain.BlocksBetweenSettlement = contractInfo.BlocksBetweenSettlement;
                _roundsUntilSettlement = Convert.ToInt32(contractInfo.BlocksBetweenSettlement) - Convert.ToInt32(numberOfRoundsAlreadyPassed);

                var stateTable = await _mainchainService.RetrieveContractState(_sidechain.ClientAccountName);
                if (stateTable.ProductionTime) await ConnectToProducers();

                await CheckContractAndUpdateWaitTimes();

                while (true)
                {
                    _timeDiff = (_sidechain.NextStateWaitEndTime * 1000) - _timeToExecuteTrx - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger.LogDebug($"timediff: {_timeDiff}");

                    if (_timeDiff <= 0)
                    {
                        if (_previousWaitTime != _sidechain.NextStateWaitEndTime || _forceTryAgain || _timeDiff + _sidechain.BlockTimeDuration <= 0) await CheckContractEndState();
                        UpdateAverageTrxTime();
                        await CheckContractAndUpdateStates();
                        await CheckContractAndUpdateWaitTimes();
                    }
                    else await Task.Delay((int)_timeDiff);

                    if (_previousWaitTime == _sidechain.NextStateWaitEndTime)
                    {
                        await Task.Delay(10);
                        _sidechain.State = SidechainPoolStateEnum.WaitForNextState;
                    }

                    TaskContainer.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Contract manager stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Contract manager crashed. {ex}");
            }
        }


        #region Auxiliar Methods

        private async Task CheckContractEndState()
        {
            var currentProducerTable = await _mainchainService.RetrieveCurrentProducer(_sidechain.ClientAccountName);
            var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.ClientAccountName);
            var stateTable = await _mainchainService.RetrieveContractState(_sidechain.ClientAccountName);
            int latestTrxTime = 0;
            _forceTryAgain = false;

            try
            {
                if (stateTable.CandidatureTime &&
                    contractInfo.CandidatureEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    await UpdateAuthorizations();
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SECRET_TIME, _sidechain.ClientAccountName);
                }
                if (stateTable.SecretTime &&
                    contractInfo.SecretEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SEND_TIME, _sidechain.ClientAccountName);
                }
                if (stateTable.IPSendTime &&
                    contractInfo.SendEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    await UpdateAuthorizations();
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_RECEIVE_TIME, _sidechain.ClientAccountName);
                }
                if (stateTable.IPReceiveTime &&
                    contractInfo.ReceiveEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    await LinkAuthorizarion(EosMsigConstants.VERIFY_BLOCK_PERMISSION, _sidechain.ClientAccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
                    await LinkAuthorizarion(EosMethodNames.HISTORY_VALIDATE, _sidechain.ClientAccountName, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.PRODUCTION_TIME, _sidechain.ClientAccountName);
                    await ConnectToProducers();
                }
                if (stateTable.ProductionTime && currentProducerTable.Any() &&
                   (currentProducerTable.Single().StartProductionTime + _sidechain.BlockTimeDuration) * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.CHANGE_CURRENT_PRODUCER, _sidechain.ClientAccountName);
                    _roundsUntilSettlement--;
                    _logger.LogDebug($"Rounds until settlement: {_roundsUntilSettlement}");
                    if (_roundsUntilSettlement == 0) await ExecuteSettlementActions();
                    await CheckPeerConnections();
                }
            }
            catch (HttpRequestException)
            {
                _logger.LogCritical($"Eos transaction failed http error. Please verify EOS endpoint. Trying again in 60 seconds.");
                await Task.Delay(60000);
                _forceTryAgain = true;
            }
            catch (ApiException apiException)
            {
                _logger.LogCritical($"Eos transaction failed http error {apiException.Message}. Please verify EOS endpoint. Trying again in 60 seconds.");
                await Task.Delay(60000);
                _forceTryAgain = true;
            }
            catch (ApiErrorException eosException)
            {
                _logger.LogCritical($"Eos transaction failed with error {eosException.error.name}. Please verify your cpu/net stake or if there is heavy congestion in the network. Trying again in 60 seconds");
                await Task.Delay(60000);
                _forceTryAgain = true;
            }

            if (latestTrxTime != 0) _latestTrxTimes.Append(latestTrxTime);
        }

        private async Task ConnectToProducers()
        {
            var ipAddresses = await GetProducersIPs();
            await _peerConnectionsHandler.ConnectToProducers(ipAddresses);
        }

        private async Task CheckContractAndUpdateWaitTimes()
        {
            _previousWaitTime = _sidechain.NextStateWaitEndTime;
            var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.ClientAccountName);
            var currentProducerList = await _mainchainService.RetrieveCurrentProducer(_sidechain.ClientAccountName);
            var currentProducer = currentProducerList.FirstOrDefault();
            if (_sidechain.State == SidechainPoolStateEnum.ConfigTime) _sidechain.NextStateWaitEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (contractInfo.CandidatureTime / 2);
            if (contractInfo.CandidatureEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.CandidatureEndDate;
            if (contractInfo.SecretEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.SecretEndDate;
            if (contractInfo.SendEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.SendEndDate;
            if (contractInfo.ReceiveEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.ReceiveEndDate;

            if (!_sidechain.ProducingBlocks) return;

            var nextBlockTime = currentProducer != null ? currentProducer.StartProductionTime + _sidechain.BlockTimeDuration : _sidechain.NextStateWaitEndTime;
            if (nextBlockTime < _sidechain.NextStateWaitEndTime || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= _sidechain.NextStateWaitEndTime)
                _sidechain.NextStateWaitEndTime = nextBlockTime;
        }

        private async Task CheckContractAndUpdateStates()
        {
            var contractState = await _mainchainService.RetrieveContractState(_sidechain.ClientAccountName);
            if (contractState.ConfigTime) _sidechain.State = SidechainPoolStateEnum.ConfigTime;
            if (contractState.CandidatureTime) _sidechain.State = SidechainPoolStateEnum.CandidatureTime;
            if (contractState.SecretTime) _sidechain.State = SidechainPoolStateEnum.SecretTime;
            if (contractState.IPSendTime) _sidechain.State = SidechainPoolStateEnum.IPSendTime;
            if (contractState.IPReceiveTime) _sidechain.State = SidechainPoolStateEnum.IPReceiveTime;

            if (contractState.ProductionTime != _sidechain.ProducingBlocks)
            {
                if (contractState.ProductionTime) _sidechain.State = SidechainPoolStateEnum.InitMining;
                _sidechain.ProducingBlocks = contractState.ProductionTime;
            }
        }

        private async Task UpdateAuthorizations()
        {
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
            var sidechainAccountInfo = await _mainchainService.GetAccount(_sidechain.ClientAccountName);
            var verifyPermissionAccounts = sidechainAccountInfo.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_BLOCK_PERMISSION).FirstOrDefault();
            if (!producerList.Any()) return;
            if (!producerList.Any(p => !_sidechain.ProducersInPool.GetEnumerable().Any(l => l.ProducerInfo.AccountName == p.Key)) &&
                producerList.Count() == verifyPermissionAccounts?.required_auth?.accounts?.Count()) return;

            var producersInPool = producerList.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    NewlyJoined = true
                }
            }).ToList();

            _sidechain.ProducersInPool.ClearAndAddRange(producersInPool);
            await _mainchainService.AuthorizationAssign(_sidechain.ClientAccountName, producerList, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
            await _mainchainService.AuthorizationAssign(_sidechain.ClientAccountName, producerList.Where(p => (ProducerTypeEnum)p.ProducerType != ProducerTypeEnum.Validator).ToList(), EosMsigConstants.VERIFY_HISTORY_PERMISSION);
        }

        private async Task LinkAuthorizarion(string actionsName, string owner, string authorization)
        {
            try
            {
                await _mainchainService.LinkAuthorization(actionsName, owner, authorization);
            }
            catch (ApiErrorException)
            {
                _logger.LogDebug("Already linked authorization");
            }
        }

        private void UpdateAverageTrxTime()
        {
            _latestTrxTimes = _latestTrxTimes.TakeLast(20);
            _timeToExecuteTrx = _latestTrxTimes.Any() ? _latestTrxTimes.Sum() / _latestTrxTimes.Count() : 0;
        }

        private async Task<IDictionary<string, IPEndPoint>> GetProducersIPs()
        {
            var ipAddressesTables = await _mainchainService.RetrieveIPAddresses(_sidechain.ClientAccountName);
            var producerEncryptedIPAdresses = ipAddressesTables.Select(t => t.EncryptedIPs[t.EncryptedIPs.Count - 1]).ToList();

            var decryptedProducerIPs = new Dictionary<string, IPEndPoint>();
            for (int i = 0; i < ipAddressesTables.Count; i++)
            {
                var producer = ipAddressesTables[i].Key;
                var producerPublicKey = ipAddressesTables[i].PublicKey;
                decryptedProducerIPs.Add(producer,
                 AssymetricEncryption.DecryptIP(producerEncryptedIPAdresses[i], _nodeConfigurations.ActivePrivateKey, producerPublicKey));

            }
            return decryptedProducerIPs;
        }
        private async Task ExecuteSettlementActions()
        {
            _logger.LogDebug("Settlement starting...");

            var producers = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
            if (producers.Where(p => p.Warning == EosTableValues.WARNING_PUNISH).Any())
            {
                _logger.LogDebug("Blacklisting producers...");
                foreach (var producer in producers)
                {
                    if (producer.Warning == EosTableValues.WARNING_PUNISH) await _mainchainService.BlacklistProducer(_sidechain.ClientAccountName, producer.Key);
                }

                await _mainchainService.PunishProd(_sidechain.ClientAccountName);
            }

            await HistoryValidationHelper.SendRequestHistoryValidation(_mainchainService, _nodeConfigurations.AccountName, _logger, producers);
            _roundsUntilSettlement = (int)_sidechain.BlocksBetweenSettlement;

            await UpdateAuthorizations();
        }

        private async Task CheckPeerConnections()
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
            var producersInPool = producersInTable.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    NewlyJoined = false,
                    IPEndPoint = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()?.IPEndPoint
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            _sidechain.ProducersInPool.ClearAndAddRange(producersInPool);

            await ConnectToProducers();

            if (_sidechain.ProducersInPool.GetEnumerable().Any(p => p.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected))
            {
                var checkConnectionTask = TaskContainer.Create(async () => await _peerConnectionsHandler.CheckConnectionStatus(_sidechain));
                checkConnectionTask.Start();
            }
        }

        #endregion Auxiliar Methods
    }
}