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
    public class SwitchProducerTurn : AbstractMainchainState<StartState, EndState>
    {
        private static Random _rnd = new Random();
        private ContractInformationTable _contractInfo;
        private ContractStateTable _contractState;
        private CurrentProducerTable _currentProducer;
        private List<BlockCountTable> _blocksCount;
        private List<ProducerInTable> _producerList;
        private int _roundsUntilSettlement;
        private bool _needsASettlement;
        private BlockheaderTable _lastBlockHeader;
        private IMainchainService _mainchainService;
        private TransactionsHandler _transactionSender;
        private NodeConfigurations _nodeConfigurations;
        private bool _removedIncludedTransactions;
        private bool _areProducersBlackListed;
        private bool _areProducersPunished;
        private bool _hasHistoryValidationBeenActivated;
        private bool _hasDoneTheSettlement;
        private bool _hasSwitchedProducer;

        public SwitchProducerTurn(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, TransactionsHandler transactionSender) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _transactionSender = transactionSender;
        }

        //TODO rpinto - this isn't finished yet
        protected override async Task DoWork()
        {
            if (_lastBlockHeader != null && !_removedIncludedTransactions)
            {
                //TODO rpinto - to remove included transactions, we have to make sure the requester has already received the produced block by p2p comm
                await _transactionSender.RemoveIncludedTransactions(_lastBlockHeader.TransactionCount, _lastBlockHeader.BlockHash);
                _removedIncludedTransactions = true;
            }

            if (IsTimeUpForProducer(_currentProducer, _contractInfo))
            {
                await _mainchainService.ExecuteChainMaintainerAction(EosMethodNames.CHANGE_CURRENT_PRODUCER, _nodeConfigurations.AccountName);
                _hasSwitchedProducer = true;
            }


            //is this if right?
            if (_needsASettlement && !_hasDoneTheSettlement)
            {
                //TODO rpinto - how do I know if they are already blacklisted
                var producersToBlackList = _producerList.Where(p => p.Warning == EosTableValues.WARNING_PUNISH).ToList();

                //TODO rpinto - can I use this list to remove blacklisted from the above list?
                var blackListedProducers = await BlackListProducers(_nodeConfigurations, producersToBlackList);
                _areProducersBlackListed = true;

                //only after the producers are all blacklisted can they be punished
                //there should be an if guarding this
                if(blackListedProducers.Count > 0)
                    await _mainchainService.PunishProd(_nodeConfigurations.AccountName);
                _areProducersPunished = true;

                //only after the producers are punished should the history validation start
                //there sould be an if guarding this
                //await SendRequestHistoryValidation(_nodeConfigurations.AccountName, _contractInfo, _producerList);
                //_hasHistoryValidationBeenActivated = true;

                _hasDoneTheSettlement = true;

            }
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(_contractState != null && _contractInfo != null && _currentProducer != null && _blocksCount != null && _contractState.ProductionTime);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {

            var settlementDone = _needsASettlement ? _hasDoneTheSettlement : true;
            return Task.FromResult((_hasSwitchedProducer && settlementDone, typeof(UpdateAuthorizationsState).Name));
        }

        protected override Task<bool> IsWorkDone()
        {
            var settlementDone = _needsASettlement ? _hasDoneTheSettlement : true;
            return Task.FromResult(_hasSwitchedProducer && settlementDone);
        }

        protected override async Task UpdateStatus()
        {
            _contractState = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
            _contractInfo = await _mainchainService.RetrieveContractInformation(_nodeConfigurations.AccountName);
            _currentProducer = await _mainchainService.RetrieveCurrentProducer(_nodeConfigurations.AccountName);
            _blocksCount = await _mainchainService.RetrieveBlockCount(_nodeConfigurations.AccountName);
            _producerList = await _mainchainService.RetrieveProducersFromTable(_nodeConfigurations.AccountName);

            if (_contractState == null || _contractInfo == null || _currentProducer == null || _blocksCount == null) return;

            var numberOfRoundsAlreadyPassed = _blocksCount.Sum(b => b.blocksproduced) + _blocksCount.Sum(b => b.blocksfailed);
            _roundsUntilSettlement = Convert.ToInt32(_contractInfo.BlocksBetweenSettlement) - Convert.ToInt32(numberOfRoundsAlreadyPassed);

            _needsASettlement = _roundsUntilSettlement -1 <= 0;

            _lastBlockHeader = await _mainchainService.GetLastValidSubmittedBlockheader(_nodeConfigurations.AccountName, (int)_contractInfo.BlocksBetweenSettlement);
        }

        private async Task SendRequestHistoryValidation(string clientAccountName, ContractInformationTable contractInfo, List<ProducerInTable> producers)
        {
            var lastValidBlockheaderTable = await _mainchainService.GetLastValidSubmittedBlockheader(clientAccountName, (int)contractInfo.BlocksBetweenSettlement);
            if (lastValidBlockheaderTable != null)
            {
                var validProducers = producers.Where(p => p.Warning != EosTableValues.WARNING_PUNISH && p.ProducerType != 1).ToList();
                if (!validProducers.Any()) return;
                var lastValidBlockheader = lastValidBlockheaderTable.ConvertToBlockHeader();
                int r = _rnd.Next(validProducers.Count);
                var chosenProducerAccountName = validProducers[r].Key;
                try
                {
                    await _mainchainService.RequestHistoryValidation(clientAccountName, chosenProducerAccountName, HashHelper.ByteArrayToFormattedHexaString(lastValidBlockheader.BlockHash));
                    _logger.LogDebug("Updated history validation table.");
                }
                catch (ApiErrorException apiException)
                {
                    _logger.LogWarning($"Unable to request history validation with error: {apiException?.error?.name}");
                }
            }
        }

        private async Task<List<ProducerInTable>> BlackListProducers(NodeConfigurations nodeConfigurations, List<ProducerInTable> producers)
        {
            var blackListed = new List<ProducerInTable>();

            if (producers.Where(p => p.Warning == EosTableValues.WARNING_PUNISH).Any())
            {
                _logger.LogDebug("Blacklisting producers...");
                foreach (var producer in producers)
                {
                    if (producer.Warning == EosTableValues.WARNING_PUNISH)
                    {
                        try
                        {
                            await _mainchainService.BlacklistProducer(nodeConfigurations.AccountName, producer.Key);
                            blackListed.Add(producer);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug($"Failed to blacklist producer {producer.Key} - {ex.Message}");
                        }
                    }
                }
            }

            return blackListed;
        }
    }
}