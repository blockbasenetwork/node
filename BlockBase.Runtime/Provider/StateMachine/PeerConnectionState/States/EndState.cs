using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.Provider.StateMachine.PeerConnectionState.States
{
    public class EndState : ProviderAbstractState<StartState, EndState, WaitForEndConfirmationState>
    {
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;
        public EndState(SidechainPool sidechainPool, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger, sidechainPool, mainchainService)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task<bool> IsWorkDone()
        {
            return Task.FromResult(true);
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            return Task.FromResult(true);
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            return Task.FromResult((true, string.Empty));
        }

        protected override Task UpdateStatus() 
        {
            
            return Task.CompletedTask;
        }

        // private void UpdateIPsInSidechain(List<IPAddressTable> IpsAddressTableEntries)
        // {
        //     if (!IpsAddressTableEntries.Any() || IpsAddressTableEntries.Any(t => !t.EncryptedIPs.Any())) return;
        //     foreach (var ipAddressTable in IpsAddressTableEntries) ipAddressTable.EncryptedIPs.RemoveAt(ipAddressTable.EncryptedIPs.Count - 1);

        //     int numberOfIpsToUpdate = (int)Math.Ceiling(_sidechainPool.ProducersInPool.Count() / 4.0);
        //     if (numberOfIpsToUpdate == 0) return;

        //     var producersInPoolList = _sidechainPool.ProducersInPool.GetEnumerable().ToList();
        //     if (!producersInPoolList.Any(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)) return;
        //     var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName)).Take(numberOfIpsToUpdate).ToList();

        //     foreach (var producer in orderedProducersInPool)
        //     {
        //         var producerIndex = orderedProducersInPool.IndexOf(producer);
        //         var producerIps = IpsAddressTableEntries.Where(p => p.Key == producer.ProducerInfo.AccountName).FirstOrDefault();
        //         if (producerIps == null || producer.ProducerInfo.IPEndPoint != null) continue;

        //         var listEncryptedIPEndPoints = producerIps.EncryptedIPs;
        //         var encryptedIpEndPoint = listEncryptedIPEndPoints[producerIndex];
        //         producer.ProducerInfo.IPEndPoint = AssymetricEncryption.DecryptIP(encryptedIpEndPoint, _nodeConfigurations.ActivePrivateKey, producer.ProducerInfo.PublicKey);
        //     }
        // }

    }

}