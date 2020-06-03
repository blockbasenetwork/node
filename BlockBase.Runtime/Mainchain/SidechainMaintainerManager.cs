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
using BlockBase.DataPersistence.ProducerData;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Network.Mainchain.Pocos;

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
        private TransactionsHandler _transactionSender;
        private HistoryValidation _historyValidation;
        private IConnector _connector;
        private IMongoDbProducerService _mongoDbProducerService;



        public TaskContainer Start()
        {
            _transactionSender.Setup().Wait();

            if(TaskContainer != null) TaskContainer.Stop();

            TaskContainer = TaskContainer.Create(async () => await SuperMethod());
            TaskContainer.Start();
            return TaskContainer;

            
        }
        public SidechainMaintainerManager(ILogger<SidechainMaintainerManager> logger, IMongoDbProducerService mongoDbService, IMainchainService mainchainService, IOptions<NodeConfigurations> nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler, TransactionsHandler transactionSender, IConnector connector)
        {
            _peerConnectionsHandler = peerConnectionsHandler;
            _mainchainService = mainchainService;
            _roundsUntilSettlement = 0;
            _logger = logger;
            _latestTrxTimes = new List<int>();
            _nodeConfigurations = nodeConfigurations.Value;
            _sidechain = new SidechainPool(_nodeConfigurations.AccountName);
            _forceTryAgain = true;
            _transactionSender = transactionSender;
            _historyValidation = new HistoryValidation(_logger, mongoDbService, _mainchainService);
            _connector = connector;
            _mongoDbProducerService = mongoDbService;
        }

        public async Task SuperMethod()
        {
            try
            {
                var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.ClientAccountName);
                var blocksCount = await _mainchainService.RetrieveBlockCount(_sidechain.ClientAccountName);
                var contractState = await _mainchainService.RetrieveContractState(_sidechain.ClientAccountName);
                var currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechain.ClientAccountName);
                

                _sidechain.BlockSizeInBytes = contractInfo.SizeOfBlockInBytes;
                _sidechain.BlockTimeDuration = contractInfo.BlockTimeDuration;
                _sidechain.BlocksBetweenSettlement = contractInfo.BlocksBetweenSettlement;

                var numberOfRoundsAlreadyPassed = blocksCount.Sum(b => b.blocksproduced) + blocksCount.Sum(b => b.blocksfailed);
                _roundsUntilSettlement = Convert.ToInt32(contractInfo.BlocksBetweenSettlement) - Convert.ToInt32(numberOfRoundsAlreadyPassed);

                //TODO rpinto - so this is done only once? shouldn't it be done periodically inside the while?
                if (contractState.ProductionTime) await ConnectToProducers();

                CheckContractAndUpdateWaitTimes(contractInfo, currentProducer);

                //TODO rpinto - this while should never be stopped unless specified by the user - the try should be on the inside
                while (true)
                {
                    
                    _timeDiff = (_sidechain.NextStateWaitEndTime * 1000) - _timeToExecuteTrx - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger.LogDebug($"timediff: {_timeDiff}");

                    if (_timeDiff <= 0)
                    {
                        UpdateAverageTrxTime();

                        currentProducer = await _mainchainService.RetrieveCurrentProducer(_sidechain.ClientAccountName);
                        contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.ClientAccountName);
                        contractState = await _mainchainService.RetrieveContractState(_sidechain.ClientAccountName);

                        //TODO rpinto - these types of ifs are difficult to understand...
                        if (_previousWaitTime != _sidechain.NextStateWaitEndTime || _forceTryAgain || _timeDiff + _sidechain.BlockTimeDuration <= 0) 
                        {
                            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
                            var sidechainAccountInfo = await _mainchainService.GetAccount(_sidechain.ClientAccountName);


                            await CheckContractEndState(contractInfo, contractState, producerList, currentProducer, sidechainAccountInfo);
                        }
                        
                        CheckContractAndUpdateStates(contractState);
                        CheckContractAndUpdateWaitTimes(contractInfo, currentProducer);

                        //TODO rpinto - why this small delay here?
                        await Task.Delay(800);
                    }
                    else await Task.Delay((int)_timeDiff);

                    if (_previousWaitTime == _sidechain.NextStateWaitEndTime)
                    {
                        //TODO rpinto - why this small delay here?
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

        private async Task CheckContractEndState(ContractInformationTable contractInfo, ContractStateTable contractStateTable, List<ProducerInTable> producerList, CurrentProducerTable currentProducerTable, EosSharp.Core.Api.v1.GetAccountResponse sidechainAccountInfo)
        {
            int latestTrxTime = 0;
            _forceTryAgain = false;

            try
            {
                //TODO rpinto - shouldn't all these if have a return at the end of their body?
                //this is risking entering many ifs given enough time to pass sufficient enddates...
                if (contractStateTable.CandidatureTime &&
                    contractInfo.CandidatureEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    await UpdateAuthorizations(producerList, sidechainAccountInfo);
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SECRET_TIME, _sidechain.ClientAccountName);
                }
                if (contractStateTable.SecretTime &&
                    contractInfo.SecretEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SEND_TIME, _sidechain.ClientAccountName);
                }
                if (contractStateTable.IPSendTime &&
                    contractInfo.SendEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    await UpdateAuthorizations(producerList, sidechainAccountInfo);
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_RECEIVE_TIME, _sidechain.ClientAccountName);
                }
                if (contractStateTable.IPReceiveTime &&
                    contractInfo.ReceiveEndDate * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    await LinkAuthorizarion(EosMsigConstants.VERIFY_BLOCK_PERMISSION, _sidechain.ClientAccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
                    await LinkAuthorizarion(EosMethodNames.HISTORY_VALIDATE, _sidechain.ClientAccountName, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.PRODUCTION_TIME, _sidechain.ClientAccountName);
                    
                    await ConnectToProducers();
                }
                if (contractStateTable.ProductionTime && currentProducerTable != null &&
                   (currentProducerTable.StartProductionTime + _sidechain.BlockTimeDuration) * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    var lastBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_sidechain.ClientAccountName, (int)_sidechain.BlocksBetweenSettlement);
                    if (lastBlockHeader != null)
                        await _transactionSender.RemoveIncludedTransactions(lastBlockHeader.TransactionCount, lastBlockHeader.BlockHash);
                    latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.CHANGE_CURRENT_PRODUCER, _sidechain.ClientAccountName);
                    _roundsUntilSettlement--;
                    _logger.LogDebug($"Rounds until settlement: {_roundsUntilSettlement}");
                    if (_roundsUntilSettlement == 0) await ExecuteSettlementActions(contractInfo, producerList, sidechainAccountInfo);
                }
                if (contractStateTable.ProductionTime && currentProducerTable != null)
                {
                    await CheckPeerConnections(producerList);
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

        private void CheckContractAndUpdateWaitTimes(ContractInformationTable contractInfo, CurrentProducerTable currentProducer)
        {
            _previousWaitTime = _sidechain.NextStateWaitEndTime;
            
            if (_sidechain.State == SidechainPoolStateEnum.ConfigTime) _sidechain.NextStateWaitEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (contractInfo.CandidatureTime / 2);
            if (contractInfo.CandidatureEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.CandidatureEndDate;
            if (contractInfo.SecretEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.SecretEndDate;
            if (contractInfo.SendEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.SendEndDate;
            if (contractInfo.ReceiveEndDate > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) _sidechain.NextStateWaitEndTime = contractInfo.ReceiveEndDate;

            if (!_sidechain.ProducingBlocks) return;

            var nextBlockTime = currentProducer != null ? currentProducer.StartProductionTime + _sidechain.BlockTimeDuration : _sidechain.NextStateWaitEndTime;
            if (nextBlockTime < _sidechain.NextStateWaitEndTime || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= _sidechain.NextStateWaitEndTime)
                _sidechain.NextStateWaitEndTime = nextBlockTime;
            if (_sidechain.NextStateWaitEndTime > DateTimeOffset.UtcNow.AddSeconds(15).ToUnixTimeSeconds())
                _sidechain.NextStateWaitEndTime = DateTimeOffset.UtcNow.AddSeconds(15).ToUnixTimeSeconds();
        }

        private void CheckContractAndUpdateStates(ContractStateTable contractState)
        {
            if (contractState.ConfigTime) _sidechain.State = SidechainPoolStateEnum.ConfigTime;
            if (contractState.CandidatureTime) _sidechain.State = SidechainPoolStateEnum.CandidatureTime;
            if (contractState.SecretTime) _sidechain.State = SidechainPoolStateEnum.SecretTime;
            if (contractState.IPSendTime) _sidechain.State = SidechainPoolStateEnum.IPSendTime;
            if (contractState.IPReceiveTime) _sidechain.State = SidechainPoolStateEnum.IPReceiveTime;

            if (contractState.ProductionTime != _sidechain.ProducingBlocks)
            {
                if (contractState.ProductionTime) _sidechain.State = SidechainPoolStateEnum.InitProduction;
                _sidechain.ProducingBlocks = contractState.ProductionTime;
            }
        }

        private async Task UpdateAuthorizations(List<ProducerInTable> producerList, EosSharp.Core.Api.v1.GetAccountResponse sidechainAccountInfo)
        {
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

            var notValidatorProducers = producerList.Where(p => (ProducerTypeEnum)p.ProducerType != ProducerTypeEnum.Validator).ToList();
            if (notValidatorProducers.Any()) await _mainchainService.AuthorizationAssign(_sidechain.ClientAccountName, notValidatorProducers, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
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

            var decryptedProducerIPs = new Dictionary<string, IPEndPoint>();
            foreach (var table in ipAddressesTables)
            {
                var producer = table.Key;
                var producerPublicKey = table.PublicKey;
                //TODO rpinto - why a list of IPs and not only one?
                var encryptedIp = table.EncryptedIPs?.LastOrDefault();
                if (encryptedIp == null) continue;

                try
                {
                    var decryptedIp = AssymetricEncryption.DecryptIP(encryptedIp, _nodeConfigurations.ActivePrivateKey, producerPublicKey);
                    decryptedProducerIPs.Add(producer, decryptedIp);
                }
                catch
                {
                    _logger.LogWarning($"Unable to decrypt IP from producer: {producer}.");
                }
            }
            return decryptedProducerIPs;
        }
        private async Task ExecuteSettlementActions(ContractInformationTable contractInfo, List<ProducerInTable> producers, EosSharp.Core.Api.v1.GetAccountResponse sidechainAccountInfo)
        {
            _logger.LogDebug("Settlement starting...");

            //TODO rpinto - commented this but I'm not sure if it needs a refresh of the list of producers after all the operations done before it
            //var producers = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
            if (producers.Where(p => p.Warning == EosTableValues.WARNING_PUNISH).Any())
            {
                _logger.LogDebug("Blacklisting producers...");
                foreach (var producer in producers)
                {
                    if (producer.Warning == EosTableValues.WARNING_PUNISH) await _mainchainService.BlacklistProducer(_sidechain.ClientAccountName, producer.Key);
                }

                await _mainchainService.PunishProd(_sidechain.ClientAccountName);
            }

            await _historyValidation.SendRequestHistoryValidation(_nodeConfigurations.AccountName, contractInfo, producers);
            _roundsUntilSettlement = (int)_sidechain.BlocksBetweenSettlement;

            await UpdateAuthorizations(producers, sidechainAccountInfo);
        }

        private async Task CheckPeerConnections(List<ProducerInTable> producers)
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();

            //TODO rpinto - commented this fetch to pass as parameter but I'm not sure it needs to be refreshed from before
            // var producers = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
            var producersInPool = producers.Select(m => new ProducerInPool
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

            //TODO rpinto - this may also take time but is awaited. Why this way here and different right below
            await ConnectToProducers();

            if (_sidechain.ProducersInPool.GetEnumerable().Any(p => p.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected))
            {
                //TODO rpinto - this returns a TaskContainer that isn't stored anywhere. So this is executed and not awaited. Is that the intended behavior?
                var checkConnectionTask = TaskContainer.Create(async () => await _peerConnectionsHandler.CheckConnectionStatus(_sidechain));
                checkConnectionTask.Start();
            }
        }

        public async Task EndSidechain()
        {
            TaskContainer.CancellationTokenSource.Cancel();
            await _mongoDbProducerService.DropRequesterDatabase(_sidechain.ClientAccountName);
        }
       
      
        #endregion Auxiliar Methods
    }
}