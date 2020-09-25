using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Helpers;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Requester.StateMachine.Common;
using BlockBase.Utils.Crypto;
using EosSharp.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Requester.StateMachine.SidechainProductionState.States
{
#pragma warning disable
    public class VerifyBlockState : AbstractMainchainState<StartState, EndState, WaitForEndConfirmationState>
    {
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private BlockheaderTable _lastBlockHeader;
        private IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private bool _hasBlockBeenVerified;
        private bool _hasEnoughSignatures;

        public VerifyBlockState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
        }

        protected override async Task DoWork()
        {
            if (!_hasBlockBeenVerified && _hasEnoughSignatures)
            {
                await _mainchainService.VerifyBlock(_nodeConfigurations.AccountName, _lastBlockHeader.Producer, _lastBlockHeader.BlockHash);
            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractState != null && _contractInfo != null && _contractState.ProductionTime && _hasEnoughSignatures);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_hasBlockBeenVerified, typeof(NextStateRouter).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            if (_hasBlockBeenVerified) return Task.FromResult(true);
            return Task.FromResult(false);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);
            _lastBlockHeader = await _mainchainService.GetLastSubmittedBlockheader(_nodeConfigurations.AccountName, Convert.ToInt32(_contractInfo.BlocksBetweenSettlement));
            _hasBlockBeenVerified = _lastBlockHeader.IsVerified;

            if (_contractState == null || _contractInfo == null) return;

            var verifySignatures = await _mainchainService.RetrieveVerifySignatures(_nodeConfigurations.AccountName);
            var producers = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);
            _hasEnoughSignatures = CheckIfBlockHasMajorityOfSignatures(verifySignatures, _lastBlockHeader.BlockHash, producers.Count, producers.Select(p => p.PublicKey).ToList());
        }

        private bool CheckIfBlockHasMajorityOfSignatures(List<VerifySignature> verifySignatureTable, string blockHash, int numberOfProducers, List<string> requiredKeys)
        {
            var verifySignatures = verifySignatureTable?.Where(t => t.BlockHash == blockHash);
            var threshold = (numberOfProducers / 2) + 1;
            var requiredSignatures = threshold > requiredKeys.Count ? requiredKeys.Count : threshold;

            return verifySignatures?.Count() >= threshold;
        }
    }
}