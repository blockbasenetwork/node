using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.StateMachine.SidechainState.States
{
    public class IPSendTimeState : AbstractState
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ContractStateTable _contractStateTable;
        private List<IPAddressTable> _ipAddressTable;
        private List<ProducerInTable> _producers;
        public IPSendTimeState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations) : base(sidechain, logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(_ipAddressTable.Where(t => t.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.EncryptedIPs.Any() ?? false);
        }

        protected override async Task DoWork()
        {
            int numberOfIpsToSend = (int)Math.Ceiling(_producers.Count() / 4.0);
            var keysToUse = ListHelper.GetListSortedCountingFrontFromIndex(_producers, _producers.FindIndex(m => m.Key == _nodeConfigurations.AccountName)).Take(numberOfIpsToSend).Select(p => p.PublicKey).ToList();
            keysToUse.Add(Sidechain.ClientPublicKey);

            var listEncryptedIps = new List<string>();
            var endpoint = _networkConfigurations.PublicIpAddress + ":" + _networkConfigurations.TcpPort;
            foreach (string receiverPublicKey in keysToUse)
            {
                listEncryptedIps.Add(AssymetricEncryption.EncryptText(endpoint, _nodeConfigurations.ActivePrivateKey, receiverPublicKey));
            }

            await _mainchainService.AddEncryptedIps(Sidechain.ClientAccountName, _nodeConfigurations.AccountName, listEncryptedIps);
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((_contractStateTable.IPSendTime || _contractStateTable.IPReceiveTime) && isProducerInTable);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((isProducerInTable && _contractStateTable.IPReceiveTime, typeof(IPReceiveState).Name));
        }

        protected override async Task UpdateStatus()
        {
            var producers = await _mainchainService.RetrieveProducersFromTable(Sidechain.ClientAccountName);
            var contractState = await _mainchainService.RetrieveContractState(Sidechain.ClientAccountName);
            var ipAddressTable = await _mainchainService.RetrieveIPAddresses(Sidechain.ClientAccountName);

            _producers = producers;
            _contractStateTable = contractState;
            _ipAddressTable = ipAddressTable;
        }
    }

}