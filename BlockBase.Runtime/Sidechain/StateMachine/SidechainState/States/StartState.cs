using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class StartState : AbstractState
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        public StartState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations): base(sidechain, logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override Task<bool> IsWorkDone()
        {
            throw new System.NotImplementedException();
        }

        protected override async Task DoWork()
        {

        }

        protected override async Task<bool> HasConditionsToContinue()
        {
            //if(Status.Sidechain.AreCommunicationsDead()) return false;
            //Needs to check other conditions
            return true;
        }
        
        protected override async Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            //TODO
            return (false, null);
        }

        protected override async Task UpdateStatus() 
        {
            //await Status.Sidechain.TryUpdateSidechainStatus(Status.Local.ClientAccountName);
        }

    }

}