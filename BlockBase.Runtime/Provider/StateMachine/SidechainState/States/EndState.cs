using System;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class EndState : AbstractState<StartState, EndState>
    {
        private bool _chainExistsInDatabase;
        private SidechainPool _sidechainPool;
        private IMongoDbProducerService _mongoDbProducerService;
        public EndState(SidechainPool sidechainPool, ILogger logger, IMongoDbProducerService mongoDbProducerService) : base(logger)
        {
            _chainExistsInDatabase = false;
            _sidechainPool = sidechainPool;
            _mongoDbProducerService = mongoDbProducerService;
        }

        protected override Task<bool> IsWorkDone()
        {
            if(!_inAutomaticMode) return Task.FromResult(true);
            if(!_chainExistsInDatabase) return Task.FromResult(true);
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
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_inAutomaticMode);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            if(!_inAutomaticMode) return Task.FromResult((true, string.Empty));
            else if(_inAutomaticMode && !_chainExistsInDatabase) return Task.FromResult((true, string.Empty));
            //it's in automatic mode and the chain still exists
            else return Task.FromResult((false, string.Empty));
        }

        protected override async Task UpdateStatus()
        {
            //if not in automatic mode, then there is nothing to be done
            if (!_inAutomaticMode) return;

            _chainExistsInDatabase = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(_sidechainPool.ClientAccountName);

        }
    }
}