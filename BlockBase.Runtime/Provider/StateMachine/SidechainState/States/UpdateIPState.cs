using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class UpdateIpState : ProviderAbstractState<StartState, EndState>
    {
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ContractStateTable _contractStateTable;
        private ContractInformationTable _contractInfo;
        private List<IPAddressTable> _ipAddressTable;
        private List<ProducerInTable> _producers;

        private bool _hasUpdatedIps;
        public UpdateIpState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations) : base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainPool = sidechainPool;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_hasUpdatedIps);
        }

        protected override async Task DoWork()
        {
            int numberOfIpsToSend = (int)Math.Ceiling(_producers.Count() / 4.0);
            var keysToUse = ListHelper.GetListSortedCountingFrontFromIndex(_producers, _producers.FindIndex(m => m.Key == _nodeConfigurations.AccountName)).Take(numberOfIpsToSend).Select(p => p.PublicKey).ToList();
            keysToUse.Add(_sidechainPool.ClientPublicKey);

            var listEncryptedIps = new List<string>();
            var endpoint = _networkConfigurations.PublicIpAddress + ":" + _networkConfigurations.TcpPort;
            foreach (string receiverPublicKey in keysToUse)
            {
                listEncryptedIps.Add(AssymetricEncryption.EncryptText(endpoint, _nodeConfigurations.ActivePrivateKey, receiverPublicKey));
            }

            var addIpsTransaction = await _mainchainService.AddEncryptedIps(_sidechainPool.ClientAccountName, _nodeConfigurations.AccountName, listEncryptedIps);

            _hasUpdatedIps = true;
            _logger.LogDebug($"Sent encrypted ips. Tx: {addIpsTransaction}");
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            if(_contractStateTable == null || _contractInfo == null || _ipAddressTable == null || _producers == null) return Task.FromResult(false);
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult(_contractStateTable.ProductionTime && isProducerInTable);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((_hasUpdatedIps, typeof(ProductionState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _ipAddressTable = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);

            //check preconditions to continue update
            if(_contractInfo == null) return;

            _delay = TimeSpan.FromSeconds(2);
        }
    }
}