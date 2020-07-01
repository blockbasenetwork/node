using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Provider.StateMachine.SidechainState.States
{
    public class ProductionState : ProviderAbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producers;
        private ContractInformationTable _contractInfo;

        private bool _hasIpChanged;

        private SidechainPool _sidechainPool;
        public ProductionState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations) : base(logger, sidechainPool)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
            _sidechainPool = sidechainPool;
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

            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            return Task.FromResult((_contractStateTable.ProductionTime || _contractStateTable.IPSendTime) && isProducerInTable);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            var isProducerInTable = _producers.Any(c => c.Key == _nodeConfigurations.AccountName);

            if (_hasIpChanged) return Task.FromResult((_hasIpChanged, typeof(UpdateIpState).Name));
            return Task.FromResult((isProducerInTable && _contractStateTable.IPSendTime, typeof(IPSendTimeState).Name));
        }

        protected override async Task UpdateStatus()
        {
            _contractInfo = await _mainchainService.RetrieveContractInformation(_sidechainPool.ClientAccountName);
            _contractStateTable = await _mainchainService.RetrieveContractState(_sidechainPool.ClientAccountName);
            _producers = await _mainchainService.RetrieveProducersFromTable(_sidechainPool.ClientAccountName);
            
            //check preconditions to continue update
            if(_contractInfo == null) return;

            //check if provider ip has changed
            var iPAddresses = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);
            var endpoint = _networkConfigurations.PublicIpAddress + ":" + _networkConfigurations.TcpPort;
            var tableEncryptedIpAddress = iPAddresses.Where(p => p.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.EncryptedIPs?.LastOrDefault();
            var encryptedIpAddress = AssymetricEncryption.EncryptText(endpoint, _nodeConfigurations.ActivePrivateKey, _sidechainPool.ClientPublicKey);
            _hasIpChanged = tableEncryptedIpAddress != encryptedIpAddress;

            _delay = _hasIpChanged ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(GetDelayInProductionTime());
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
    }

}