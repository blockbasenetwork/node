using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Endpoints;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class NextStateRouter : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private string _nextState;
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private IMainchainService _mainchainService;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private bool _timeForConfigAutoUpdate;

        private NodeConfigurations _nodeConfigurations;
        private RequesterConfigurations _requesterConfigurations;

        public NextStateRouter(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IMongoDbRequesterService mongoDbRequesterService, RequesterConfigurations requesterConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbRequesterService = mongoDbRequesterService;
            _requesterConfigurations = requesterConfigurations;
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractState != null);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_nextState != null, _nextState));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(false);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);

            if (_contractState == null || _contractInfo == null) return;

            if (_requesterConfigurations.BBTValueAutoConfig) await CheckIfIsTimeToUpdateBBTValue();

            _nextState = GetNextSidechainState(_contractInfo, _contractState);

            if (_nextState == null)
            {
                _logger.LogDebug($"{this.GetType().Name} - Nothing to do to maintain...delaying");
                _delay = TimeSpan.FromSeconds(10);
            }
            else
            {
                _delay = TimeSpan.FromSeconds(3);
            }
        }

        private string GetNextSidechainState(ContractInformationTable contractInfo, ContractStateTable contractState)
        {
            if (_timeForConfigAutoUpdate)
                return typeof(AutomaticConfigUpdateState).Name;
            if (contractState.ConfigTime)
                return typeof(CandidatureReceivalState).Name;
            if (contractState.CandidatureTime && IsTimeUpForSidechainPhase(contractInfo.CandidatureEndDate, 0))
                return typeof(SecretSharingState).Name;
            if (contractState.SecretTime && IsTimeUpForSidechainPhase(contractInfo.SecretEndDate, 0))
                return typeof(IPSharingState).Name;
            if (contractState.IPSendTime && IsTimeUpForSidechainPhase(contractInfo.SendEndDate, 0))
                return typeof(ProvidersConnectionState).Name;
            if (contractState.IPReceiveTime && IsTimeUpForSidechainPhase(contractInfo.ReceiveEndDate, 0))
                return typeof(StartProductionState).Name;
            return null;
        }

        private TimeSpan CalculateNextDelay()
        {
            return TimeSpan.FromMinutes(1);
            //TODO
        }

        private async Task CheckIfIsTimeToUpdateBBTValue()
        {
            try
            {
                var latestStoredBBTValue = await _mongoDbRequesterService.GetLatestBBTValue();
                if (latestStoredBBTValue == null || Convert.ToInt64(latestStoredBBTValue.Timestamp) < DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds())
                {
                    var currentBBTValue = await GetCurrentBBTValue();
                    await _mongoDbRequesterService.AddBBTValueToDatabaseAsync(currentBBTValue);
                    if (latestStoredBBTValue != null) _timeForConfigAutoUpdate = true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to update BBT Value");
                _logger.LogDebug($"Exception: {e}");
            }
        }

        private async Task<double> GetCurrentBBTValue()
        {
            var request = HttpHelper.ComposeWebRequestGet(BlockBaseNetworkEndpoints.GET_CURRENT_BBT_VALUE);
            var response = await HttpHelper.CallWebRequest(request);

            return JsonConvert.DeserializeObject<double>(response);
        }
    }
}