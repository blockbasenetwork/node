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

namespace BlockBase.Runtime.Mainchain
{
    public class SidechainMaintainerManager : IThreadableComponent
    {
        public SidechainPool _sidechain { get; set; }
        public TaskContainer TaskContainer { get; private set; }
        private IMainchainService _mainchainService;
        private long _timeDiff;
        private int _timeToExecuteTrx;
        private int _roundsUntilSettlement;
        private IEnumerable<int> _latestTrxTimes;
        private double _previousWaitTime;
        private ILogger _logger;
        private const float DELAY_IN_SECONDS = 0.5f;

        public TaskContainer Start()
        {
            TaskContainer = TaskContainer.Create(async () => await SuperMethod());
            TaskContainer.Start();
            return TaskContainer;
        }

        public SidechainMaintainerManager(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService)
        {
            _sidechain = sidechain;
            _mainchainService = mainchainService;
            _roundsUntilSettlement = (int)sidechain.BlocksBetweenSettlement;
            _logger = logger;
            _latestTrxTimes = new List<int>();
        }

        public async Task SuperMethod()
        {
            try
            {
                var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.SidechainName);
                _sidechain.BlockTimeDuration = contractInfo.BlockTimeDuration;
                _sidechain.BlocksBetweenSettlement = contractInfo.BlocksBetweenSettlement;

                while (true)
                {
                    _timeDiff = (_sidechain.NextStateWaitEndTime * 1000) - _timeToExecuteTrx - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _logger.LogDebug($"timediff: {_timeDiff}");

                    if (_timeDiff <= 0)
                    {
                        if (_previousWaitTime != _sidechain.NextStateWaitEndTime) await CheckContractEndState();
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
            var currentProducerTable = await _mainchainService.RetrieveCurrentProducer(_sidechain.SidechainName);
            var stateTable = await _mainchainService.RetrieveContractState(_sidechain.SidechainName);
            int latestTrxTime = 0;

            if (stateTable.CandidatureTime &&
               _sidechain.NextStateWaitEndTime * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SECRET_TIME, _sidechain.SidechainName);
            }
            if (stateTable.SecretTime &&
               _sidechain.NextStateWaitEndTime * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_SEND_TIME, _sidechain.SidechainName);
                await UpdateAuthorization(_sidechain.SidechainName);
            }
            if (stateTable.IPSendTime &&
               _sidechain.NextStateWaitEndTime * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.START_RECEIVE_TIME, _sidechain.SidechainName);
                await LinkAuthorizarion(EosMsigConstants.VERIFY_BLOCK_PERMISSION, _sidechain.SidechainName);
            }
            if (stateTable.IPReceiveTime &&
               _sidechain.NextStateWaitEndTime * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.PRODUTION_TIME, _sidechain.SidechainName);
            }
            if (stateTable.ProductionTime &&
               (currentProducerTable.Single().StartProductionTime + _sidechain.BlockTimeDuration) * 1000 - _timeToExecuteTrx <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                latestTrxTime = await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.CHANGE_CURRENT_PRODUCER, _sidechain.SidechainName);
                _roundsUntilSettlement--;
                if (_roundsUntilSettlement == 0) await ExecuteSettlementActions();
            }

            if (latestTrxTime != 0) _latestTrxTimes.Append(latestTrxTime);
        }

        private async Task CheckContractAndUpdateWaitTimes()
        {
            _previousWaitTime = _sidechain.NextStateWaitEndTime;
            var contractInfo = await _mainchainService.RetrieveContractInformation(_sidechain.SidechainName);
            var currentProducerList = await _mainchainService.RetrieveCurrentProducer(_sidechain.SidechainName);
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
            var contractState = await _mainchainService.RetrieveContractState(_sidechain.SidechainName);
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

        private async Task UpdateAuthorization(string accountName)
        {
            var producerList = await _mainchainService.RetrieveProducersFromTable(_sidechain.SidechainName);
            await _mainchainService.AuthorizationAssign(accountName, producerList);
        }
        
        private async Task LinkAuthorizarion(string actionsName, string owner)
        {
            try
            {
                await _mainchainService.LinkAuthorization(actionsName, owner);
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

        private async Task ExecuteSettlementActions()
        {
            _roundsUntilSettlement = (int)_sidechain.BlocksBetweenSettlement;
            await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.BLACKLIST_PRODUCERS, _sidechain.SidechainName);
            await _mainchainService.PunishProd(_sidechain.SidechainName);
        }

        #endregion Auxiliar Methods
    }
}