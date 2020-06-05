using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.SidechainMaintainerState.States
{
    public class UpdateAuthorizationsState : AbstractMainchainState<StartState, EndState>
    {
        private bool _inNeedToUpdateAuthorizations;
        private SidechainPool _sidechainPool;
        private List<ProducerInTable> _producerList;
        private IMainchainService _mainchainService;
        private EosSharp.Core.Api.v1.GetAccountResponse _sidechainAccountInfo;
        public UpdateAuthorizationsState(ILogger logger, SidechainPool sidechainPool, IMainchainService mainchainService) : base(logger)
        {
            _inNeedToUpdateAuthorizations = false;
            _sidechainPool = sidechainPool;
            _mainchainService = mainchainService;
        }

        protected override async Task DoWork()
        {

            var producersInPool = _producerList.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    //TODO rpinto - why is this here set to true??
                    NewlyJoined = true
                }
            }).ToList();

            _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);

            var notValidatorProducers = _producerList.Where(p => (ProducerTypeEnum)p.ProducerType != ProducerTypeEnum.Validator).ToList();
            
            //TODO rpinto - these are two different authorization assign where if the first is done but the second fails, the second will never go through again because the first will always blow up
            //used try catch to try to solve it
            try {
                await _mainchainService.AuthorizationAssign(_sidechainPool.ClientAccountName, _producerList, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
            } catch(Exception ex)
            {
                _logger.LogError($"Failed in assigning permissions", ex.Message);
            }
            try
            {
                if (notValidatorProducers.Any()) await _mainchainService.AuthorizationAssign(_sidechainPool.ClientAccountName, notValidatorProducers, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
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
            _producerList = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _sidechainAccountInfo = await _mainchainService.GetAccount(_sidechainPool.ClientAccountName);
            _inNeedToUpdateAuthorizations = DoesItNeedToUpdateAuthorizations(_producerList, _sidechainAccountInfo, _sidechainPool);

            //TODO rpinto - what it needs to do is to get the sidechain account and check if the permissions associated are there!

        }

        private bool DoesItNeedToUpdateAuthorizations(List<ProducerInTable> producerList, EosSharp.Core.Api.v1.GetAccountResponse sidechainAccountInfo, SidechainPool sidechainPool)
        {
            //TODO rpinto - these checks here don't seem right to me
            var verifyPermissionAccounts = sidechainAccountInfo.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_BLOCK_PERMISSION).FirstOrDefault();
            if (!producerList.Any()) return false;
            if (!producerList.Any(p => !sidechainPool.ProducersInPool.GetEnumerable().Any(l => l.ProducerInfo.AccountName == p.Key)) &&
                producerList.Count() == verifyPermissionAccounts?.required_auth?.accounts?.Count()) return false;

            return true;
        }

    }
}