using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Mainchain.StateMachine.States
{
    public class UpdateAuthorizationsState : AbstractMainchainState<StartState, EndState>
    {
        public UpdateAuthorizationsState(ILogger logger) : base(logger)
        {
        }

        protected override Task DoWork()
        {
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            //TODO rpinto - how to check if authorizations were updated?
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            throw new System.NotImplementedException();


            //get account à conta sidechain e verificar se estão lá as permissões que criámos
        }


        // private async Task UpdateAuthorizations(List<ProducerInTable> producerList, EosSharp.Core.Api.v1.GetAccountResponse sidechainAccountInfo, SidechainPool sidechainPool)
        // {
        //     //TODO rpinto - these checks here don't seem completely right to me
        //     var verifyPermissionAccounts = sidechainAccountInfo.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_BLOCK_PERMISSION).FirstOrDefault();
        //     if (!producerList.Any()) return;
        //     if (!producerList.Any(p => !sidechainPool.ProducersInPool.GetEnumerable().Any(l => l.ProducerInfo.AccountName == p.Key)) &&
        //         producerList.Count() == verifyPermissionAccounts?.required_auth?.accounts?.Count()) return;

        //     var producersInPool = producerList.Select(m => new ProducerInPool
        //     {
        //         ProducerInfo = new ProducerInfo
        //         {
        //             AccountName = m.Key,
        //             PublicKey = m.PublicKey,
        //             ProducerType = (ProducerTypeEnum)m.ProducerType,
        //             NewlyJoined = true
        //         }
        //     }).ToList();

        //     var notValidatorProducers = producerList.Where(p => (ProducerTypeEnum)p.ProducerType != ProducerTypeEnum.Validator).ToList();
        //     sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
        //     await _mainchainService.AuthorizationAssign(sidechainPool.ClientAccountName, producerList, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
        //     if (notValidatorProducers.Any()) await _mainchainService.AuthorizationAssign(sidechainPool.ClientAccountName, notValidatorProducers, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
        // }
    }
}