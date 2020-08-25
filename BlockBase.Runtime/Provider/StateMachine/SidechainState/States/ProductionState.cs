using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class ProductionState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private ContractInformationTable _contractInfo;
        private List<IPAddressTable> _ipAddresses;
        private List<WarningTable> _warnings;
        private List<BlackListTable> _blacklist;
        private bool _exitRequested;

        private IMongoDbProducerService _mongoDbProducerService;

        private bool _needsToUpdateIps;
        private bool _needsToUpdatePublicKey;

        public ProductionState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, IMongoDbProducerService mongoDbProducerService) : base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainPool = sidechainPool;
            _mongoDbProducerService = mongoDbProducerService;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task DoWork()
        {
            return default(Task);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _contractInfo == null ||  _producers == null) return Task.FromResult(false);
            return Task.FromResult((_contractStateTable.ProductionTime || _contractStateTable.IPSendTime));
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);
            var isProducerInBlacklist = _blacklist.Any(b => b.Key == _nodeConfigurations.AccountName);

            if (_needsToUpdateIps) return Task.FromResult((_needsToUpdateIps, typeof(UpdateIpState).Name));
            if (_needsToUpdatePublicKey) return Task.FromResult((_needsToUpdatePublicKey, typeof(UpdateKeyState).Name));
            if (!isProducerInTable && !isProducerInBlacklist && !_exitRequested) return Task.FromResult((true, typeof(StartState).Name));
            if (!isProducerInTable && (isProducerInBlacklist || _exitRequested)) return Task.FromResult((true, typeof(EndState).Name));
            return Task.FromResult((isProducerInTable && _contractStateTable.IPSendTime, typeof(IPSendTimeState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _ipAddresses = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);
            _warnings = await _mainchainService.RetrieveWarningTable(_sidechainPool.ClientAccountName);
            _blacklist = await _mainchainService.RetrieveBlacklistTable(_sidechainPool.ClientAccountName);
            
            //check preconditions to continue update
            if(_contractInfo == null) return;
            if(!_producers.Any(c => c.Key == _nodeConfigurations.AccountName)) return;

            _needsToUpdateIps = IsIpUpdateRequired(_ipAddresses.Where(t => t.Key == _nodeConfigurations.AccountName).SingleOrDefault().EncryptedIPs);
            _needsToUpdatePublicKey = _producers.Where(p => p.Key == _nodeConfigurations.AccountName).SingleOrDefault().PublicKey != _nodeConfigurations.ActivePublicKey;
            await UpdatePastSidechainDbBasedOnWarnings();

            _exitRequested = await CheckIfExitHasBeenRequested();
            _delay = _needsToUpdateIps ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(GetDelayInProductionTime());
        }

        private int GetDelayInProductionTime()
        {
            var candidatureTimediff = _contractInfo.CandidatureEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var secretTimediff = _contractInfo.SecretEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var ipSendTimediff = _contractInfo.SendEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var ipReceiveTimediff = _contractInfo.ReceiveEndDate - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (candidatureTimediff > 0) return Convert.ToInt32(candidatureTimediff);
            if (secretTimediff > 0) return Convert.ToInt32(secretTimediff);
            if (ipSendTimediff > 0) return Convert.ToInt32(ipSendTimediff);
            if (ipReceiveTimediff > 0) return Convert.ToInt32(ipReceiveTimediff);

            return Convert.ToInt32(15);
        }

        private bool IsIpUpdateRequired(List<string> encryptedIpsInTable)
        {
            int numberOfIpsToSend = (int)Math.Ceiling(_producers.Count() / 4.0);
            var keysToUse = ListHelper.GetListSortedCountingFrontFromIndex(_producers, _producers.FindIndex(m => m.Key == _nodeConfigurations.AccountName)).Take(numberOfIpsToSend).Select(p => p.PublicKey).ToList();
            keysToUse.Add(_sidechainPool.ClientPublicKey);

            var listEncryptedIps = new List<string>();
            var endpoint = _networkConfigurations.GetResolvedIp() + ":" + _networkConfigurations.TcpPort;
            foreach (string receiverPublicKey in keysToUse)
            {
                listEncryptedIps.Add(AssymetricEncryption.EncryptText(endpoint, _nodeConfigurations.ActivePrivateKey, receiverPublicKey));
            }

            return (listEncryptedIps.Except(encryptedIpsInTable).Any() || encryptedIpsInTable.Except(listEncryptedIps).Any());
        }

        private async Task UpdatePastSidechainDbBasedOnWarnings()
        {
            var existingPastSidechain = await _mongoDbProducerService.GetPastSidechainAsync(_sidechainPool.ClientAccountName, _sidechainPool.SidechainCreationTimestamp);
            if (existingPastSidechain != null && existingPastSidechain.ReasonLeft == LeaveNetworkReasonsConstants.EXIT_REQUEST) return;

            var warningsForThisProvider = _warnings.Where(w => w.Producer == _nodeConfigurations.AccountName);

            if (!warningsForThisProvider.Any())
            {
                await _mongoDbProducerService.RemovePastSidechainFromDatabaseAsync(_sidechainPool.ClientAccountName, _sidechainPool.SidechainCreationTimestamp);
                return;
            }
            var oldestWarning = warningsForThisProvider.First();

            foreach (var warning in warningsForThisProvider)
            {
                if (warning.WarningCreationDateInSeconds < oldestWarning.WarningCreationDateInSeconds)
                    oldestWarning = warning;
            }

            var reasonLeft = oldestWarning.WarningType == (int)WarningTypeEnum.FailedToProduceBlocks ? LeaveNetworkReasonsConstants.FAILED_TO_PRODUCE_BLOCKS :
                             oldestWarning.WarningType == (int)WarningTypeEnum.FailedToValidateHistory ? LeaveNetworkReasonsConstants.FAILED_TO_VALIDATE_HISTORY : null;
            await _mongoDbProducerService.AddPastSidechainToDatabaseAsync(_sidechainPool.ClientAccountName, _sidechainPool.SidechainCreationTimestamp, false, reasonLeft);
        }

        private async Task<bool> CheckIfExitHasBeenRequested()
        {
            var pastSidechainInDb = await _mongoDbProducerService.GetPastSidechainAsync(_sidechainPool.ClientAccountName, _sidechainPool.SidechainCreationTimestamp);
            return (pastSidechainInDb != null && pastSidechainInDb.ReasonLeft == LeaveNetworkReasonsConstants.EXIT_REQUEST);
        }
    }
}