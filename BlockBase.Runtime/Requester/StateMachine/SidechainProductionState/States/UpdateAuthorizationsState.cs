using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Requester.StateMachine.Common;
using EosSharp.Core.Api.v1;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainProductionState.States
{
    public class UpdateAuthorizationsState : AbstractMainchainState<StartState, EndState>
    {
        private bool _inNeedToUpdateAuthorizations;
        private NodeConfigurations _nodeConfigurations;
        private List<ProducerInTable> _producerList;
        private IMainchainService _mainchainService;
        private EosSharp.Core.Api.v1.GetAccountResponse _sidechainAccountInfo;

        public UpdateAuthorizationsState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _inNeedToUpdateAuthorizations = false;
            _nodeConfigurations = nodeConfigurations;
            _mainchainService = mainchainService;
        }

        protected override async Task DoWork()
        {

            //TODO rpinto - this is done in the old version - doesn't seem right to me
            // var producersInPool = _producerList.Select(m => new ProducerInPool
            // {
            //     ProducerInfo = new ProducerInfo
            //     {
            //         AccountName = m.Key,
            //         PublicKey = m.PublicKey,
            //         ProducerType = (ProducerTypeEnum)m.ProducerType,
            //         //TODO rpinto - why is this here set to true??
            //         NewlyJoined = true
            //     }
            // }).ToList();

            //  _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);




            var notValidatorProducers = _producerList.Where(p => (ProducerTypeEnum)p.ProducerType != ProducerTypeEnum.Validator).ToList();
            
            //TODO rpinto - these are two different authorization assign where if the first is done but the second fails, the second will never go through again because the first will always blow up
            //used try catch to try to solve it
            try {
                await _mainchainService.AuthorizationAssign(_nodeConfigurations.AccountName, _producerList, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
            } catch(Exception ex)
            {
                _logger.LogError($"Failed in assigning permissions", ex.Message);
            }
            try
            {
                if (notValidatorProducers.Any()) await _mainchainService.AuthorizationAssign(_nodeConfigurations.AccountName, notValidatorProducers, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
            } catch(Exception ex)
            {
                _logger.LogError($"Failed in assigning permissions", ex.Message);
            }

            
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO rpinto - how do we determine if it has conditions to continue without using only _iNeedToUpdateAuthoriations
            //without a good check here this could result in an infinite loop if permissions are unable to be set!
            return Task.FromResult(true);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((!_inNeedToUpdateAuthorizations, typeof(NextStateRouter).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(!_inNeedToUpdateAuthorizations);
        }

        protected override async Task UpdateStatus()
        {
            _producerList = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
            _sidechainAccountInfo = await _mainchainService.GetAccount(_nodeConfigurations.AccountName);
            _inNeedToUpdateAuthorizations = DoesItNeedToUpdateAuthorizations(_producerList, _sidechainAccountInfo);
        }

        private bool DoesItNeedToUpdateAuthorizations(List<ProducerInTable> producerList, EosSharp.Core.Api.v1.GetAccountResponse sidechainAccountInfo)
        {
            var verifyBlockPermission = sidechainAccountInfo.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_BLOCK_PERMISSION).FirstOrDefault();
            //TODO rpinto - remove old version of verifyhistori
            var verifyHistoryPermisson = sidechainAccountInfo.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_HISTORY_PERMISSION || p.perm_name == "verifyhistori").FirstOrDefault();
            if (!producerList.Any()) return false;
            foreach(var producer in producerList)
            {
                if(!DoesProducerHavePermission(producer, verifyBlockPermission)) return true;
                if((producer.ProducerType == 2 || producer.ProducerType == 3) && !DoesProducerHavePermission(producer, verifyHistoryPermisson)) return true;
            }
            return false;
        }

        private bool DoesProducerHavePermission(ProducerInTable producer, Permission permission)
        {
            if(permission == null) return false;

            return permission.required_auth.accounts.Any(a => a.permission.actor == producer.Key && a.permission.permission == "active" && a.weight == 1);
        }

    }
}