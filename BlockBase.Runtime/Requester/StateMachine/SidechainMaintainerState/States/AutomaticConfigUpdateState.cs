using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Requester.StateMachine.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainMaintainerState.States
{
    public class AutomaticConfigUpdateState : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private IMainchainService _mainchainService;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private NodeConfigurations _nodeConfigurations;
        private ContractInformationTable _contractInfoTable;
        private ChangeConfigurationTable _changeConfigTable;
        private bool _configChangeExists;

        public AutomaticConfigUpdateState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, IMongoDbRequesterService mongoDbRequesterService) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbRequesterService = mongoDbRequesterService;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_configChangeExists);
        }

        protected override async Task DoWork()
        {
            await UpdateConfigurations();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractInfoTable != null);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_configChangeExists, typeof(NextStateRouter).Name));
        }

        protected override async Task UpdateStatus()
        {
            _changeConfigTable = await _mainchainService.RetrieveConfigurationChanges(_nodeConfigurations.AccountName);
            _contractInfoTable = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);

            if (_changeConfigTable != null) _configChangeExists = true;

            _delay = TimeSpan.FromSeconds(10);
        }

        private async Task UpdateConfigurations()
        {
            var latestBBTValue = await _mongoDbRequesterService.GetLatestBBTValue();
            var previousBBTValue = await _mongoDbRequesterService.GetPreviousWeekBBTValue();

            if (previousBBTValue != null)
            {
                var convertedLatestBBTValue = Convert.ToUInt64(10000 * latestBBTValue.ValueInUSD);
                var convertedPreviousBBTValue = Convert.ToUInt64(10000 * previousBBTValue.ValueInUSD);

                var configurationChanges = new ChangeConfigurationTable();

                configurationChanges.Key = _nodeConfigurations.AccountName;

                configurationChanges.BlockTimeDuration = _contractInfoTable.BlockTimeDuration;
                configurationChanges.SizeOfBlockInBytes = _contractInfoTable.SizeOfBlockInBytes;
                configurationChanges.NumberOfFullProducersRequired = _contractInfoTable.NumberOfFullProducersRequired;
                configurationChanges.NumberOfHistoryProducersRequired = _contractInfoTable.NumberOfHistoryProducersRequired;
                configurationChanges.NumberOfValidatorProducersRequired = _contractInfoTable.NumberOfValidatorProducersRequired;

                configurationChanges.MaxPaymentPerBlockFullProducers = (_contractInfoTable.MaxPaymentPerBlockFullProducers * convertedPreviousBBTValue) / convertedLatestBBTValue;
                configurationChanges.MaxPaymentPerBlockHistoryProducers = (_contractInfoTable.MaxPaymentPerBlockHistoryProducers * convertedPreviousBBTValue) / convertedLatestBBTValue;
                configurationChanges.MaxPaymentPerBlockValidatorProducers = (_contractInfoTable.MaxPaymentPerBlockValidatorProducers * convertedPreviousBBTValue) / convertedLatestBBTValue;
                configurationChanges.MinPaymentPerBlockFullProducers = (_contractInfoTable.MinPaymentPerBlockFullProducers * convertedPreviousBBTValue) / convertedLatestBBTValue;
                configurationChanges.MinPaymentPerBlockHistoryProducers = (_contractInfoTable.MinPaymentPerBlockHistoryProducers * convertedPreviousBBTValue) / convertedLatestBBTValue;
                configurationChanges.MinPaymentPerBlockValidatorProducers = (_contractInfoTable.MinPaymentPerBlockValidatorProducers * convertedPreviousBBTValue) / convertedLatestBBTValue;
                configurationChanges.Stake = (_contractInfoTable.Stake * convertedPreviousBBTValue) / convertedLatestBBTValue;

                var mappedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configurationChanges));

                var alterConfigTx = await _mainchainService.AlterConfigurations(_nodeConfigurations.AccountName, mappedConfig);
            }
            else
            {
                _configChangeExists = true;
            }
        }
    }
}