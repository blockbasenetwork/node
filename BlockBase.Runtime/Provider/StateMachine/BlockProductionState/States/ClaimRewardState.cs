using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.BlockProductionState.States
{
    public class ClaimRewardState : ProviderAbstractState<StartState, EndState>
    {
        private IMainchainService _mainchainService;
        private SidechainPool _sidechainPool;
        private NodeConfigurations _nodeConfigurations;
        private ContractStateTable _contractStateTable;
        private List<RewardTable> _rewardList;

        private bool _hasTriedToRetrieveRewardOnce;

        public ClaimRewardState(ILogger logger, NodeConfigurations nodeConfigurations, IMainchainService mainchainService, SidechainPool sidechainPool) : base(logger, sidechainPool)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechainPool;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override async Task DoWork()
        {
            try
            {
                var rewardToClaim = _rewardList.SingleOrDefault(p => p.Key == _sidechainPool.ClientAccountName);
                if(rewardToClaim != null && rewardToClaim.Reward > 0)
                {
                    await _mainchainService.ClaimReward(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName);
                    _logger.LogInformation($"Claimed {Math.Round((double)rewardToClaim.Reward / 10000,4)} BBT");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Unable to claim reward: {ex.Message}");
            }
            finally
            {
                _hasTriedToRetrieveRewardOnce = true;
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _rewardList == null) return Task.FromResult(false);
            return Task.FromResult(true);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_hasTriedToRetrieveRewardOnce, typeof(StartState).Name));
        }


        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_hasTriedToRetrieveRewardOnce);
        }

        protected override async Task UpdateStatus()
        {
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _rewardList = await _mainchainService.RetrieveRewardTable(_nodeConfigurations.AccountName);
        }
    }
}