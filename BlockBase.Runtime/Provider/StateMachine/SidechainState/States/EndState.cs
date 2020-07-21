using System;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class EndState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private bool _chainExistsInDatabase;
        private IMongoDbProducerService _mongoDbProducerService;
        private ISidechainProducerService _sidechainProducerService;

        private bool _inAutomaticMode;
        private bool _didWorkOnce;

        public EndState(SidechainPool sidechainPool, ILogger logger, IMongoDbProducerService mongoDbProducerService, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, bool inAutomaticMode) : base(logger, sidechainPool, mainchainService)
        {
            _chainExistsInDatabase = false;
            _sidechainPool = sidechainPool;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainProducerService = sidechainProducerService;
            _inAutomaticMode = inAutomaticMode;
        }

        protected override Task<bool> IsWorkDone()
        {
            if (!_chainExistsInDatabase) return Task.FromResult(true);
            return Task.FromResult(false);
        }

        protected override async Task DoWork()
        {
            //the sidechain data should only be deleted if the producer is in automatic mode
            //otherwise, it's never deleted, even if he's not a candidate or producer
            if (_inAutomaticMode)
            {
                _logger.LogDebug($"Removing sidechain {_sidechainPool.ClientAccountName} data from database");
                await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(_sidechainPool.ClientAccountName);
            }

            await _mongoDbProducerService.AddPastSidechainToDatabaseAsync(_sidechainPool.ClientAccountName, _sidechainPool.SidechainCreationTimestamp, true);

            _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(_sidechainPool.ClientAccountName);
            _didWorkOnce = true;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(true);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if (_inAutomaticMode && !_chainExistsInDatabase) return Task.FromResult((true, string.Empty));
            //it's in automatic mode and the chain still exists
            else return Task.FromResult((_didWorkOnce, string.Empty));
        }

        protected override async Task UpdateStatus()
        {
            //if not in automatic mode, then there is nothing to be done
            if (!_inAutomaticMode) return;

            _chainExistsInDatabase = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_sidechainPool.ClientAccountName);

        }
    }
}