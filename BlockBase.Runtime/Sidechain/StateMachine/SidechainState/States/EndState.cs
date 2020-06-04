using System;
using System.Threading.Tasks;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class EndState : AbstractState<StartState, EndState>
    {
        public EndState(SidechainPool sidechain, ILogger logger) : base(logger)
        {
            
        }

        protected override Task<bool> IsWorkDone()
        {
            throw new System.NotImplementedException();
        }

        protected override Task DoWork()
        {
            
            throw new NotImplementedException();
            //TODO check if there is something to be deleted
            // if(_sidechainProducerService.DoesChainExist(sidechainName))
            //     {
            //         //if chain exists in pool and isn't running, remove it
            //         //this also means that there should be remnants of the database
            //         _logger.LogDebug($"Removing sidechain {sidechainName} execution engine");
            //         _sidechainProducerService.RemoveSidechainFromProducerAndStopIt(sidechainName);
            //     }
       
            //     var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(sidechainName);
                
            //     //rpinto - if the endchain request is done manually, and the cleanLocalSidechanData is set to true, it should delete the data
            //     if (chainExistsInDb && cleanLocalSidechainData) 
            //     {
            //         _logger.LogDebug($"Removing sidechain {sidechainName} data from database");
            //         await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(sidechainName);
            //     }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            
            throw new System.NotImplementedException();
        }
    }
}