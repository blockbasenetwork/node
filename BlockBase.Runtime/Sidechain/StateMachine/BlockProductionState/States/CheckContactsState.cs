using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Common;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.StateMachine.SidechainState;
using BlockBase.Runtime.StateMachine.SidechainState.States;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using static BlockBase.Network.PeerConnection;

namespace BlockBase.Runtime.StateMachine.BlockProductionState.States
{
    public class CheckContactsState : AbstractState<StartState, EndState>
    {

        private IMainchainService _mainchainService;
        private ContractStateTable _contractStateTable;
        private List<ProducerInTable> _producerList;
        private SidechainPool _sidechainPool;
        private NodeConfigurations _nodeConfigurations;
        private PeerConnectionsHandler _peerConnectionsHandler;

        public CheckContactsState(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, SidechainPool sidechainPool, PeerConnectionsHandler peerConnectionsHandler) : base(logger)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _sidechainPool = sidechainPool;
            _peerConnectionsHandler = peerConnectionsHandler;
        }

        protected override Task DoWork()
        {
            //checks comms and updates to whom he should be connected to
            throw new System.NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            //TODO verifies if he is a producer and the sidechain is in production state
            //if he has no contacts there shouldn't be no condition to continue
            throw new System.NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {

            //jumps to the SynchronizeNodeState
            throw new System.NotImplementedException();
        }

        protected override Task<bool> IsWorkDone()
        {
            //there isn't a clear rule to determine if the work is done because he could always improve a little in subsequent runs
            throw new System.NotImplementedException();
        }

        protected override Task UpdateStatus()
        {
            //fetches contact data
            throw new System.NotImplementedException();
        }


        private async Task CheckPeerConnections(List<ProducerInTable> producers)
        {
            var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();

            //TODO rpinto - commented this fetch to pass as parameter but I'm not sure it needs to be refreshed from before
            // var producers = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
            var producersInPool = producers.Select(m => new ProducerInPool
            {
                ProducerInfo = new ProducerInfo
                {
                    AccountName = m.Key,
                    PublicKey = m.PublicKey,
                    ProducerType = (ProducerTypeEnum)m.ProducerType,
                    NewlyJoined = false,
                    IPEndPoint = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()?.IPEndPoint
                },
                PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
            }).ToList();

            _sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);

            //TODO rpinto - this may also take time but is awaited. Why this way here and different right below
            await _peerConnectionsHandler.ConnectToProducers(await GetProducersIPs());

            if (_sidechainPool.ProducersInPool.GetEnumerable().Any(p => p.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected))
            {
                //TODO rpinto - this returns a TaskContainer that isn't stored anywhere. So this is executed and not awaited. Is that the intended behavior?

                //TODO rpinto - this is an unawaited check - refactor to a awaited call that fires in parallel multiple ping pong checks
                _peerConnectionsHandler.CheckConnectionStatus(_sidechainPool);
            }


        }

        private async Task<IDictionary<string, IPEndPoint>> GetProducersIPs()
        {
            var ipAddressesTables = await _mainchainService.RetrieveIPAddresses(_sidechainPool.ClientAccountName);

            var decryptedProducerIPs = new Dictionary<string, IPEndPoint>();
            foreach (var table in ipAddressesTables)
            {
                var producer = table.Key;
                var producerPublicKey = table.PublicKey;
                //TODO rpinto - why a list of IPs and not only one?
                var encryptedIp = table.EncryptedIPs?.LastOrDefault();
                if (encryptedIp == null) continue;

                try
                {
                    var decryptedIp = AssymetricEncryption.DecryptIP(encryptedIp, _nodeConfigurations.ActivePrivateKey, producerPublicKey);
                    decryptedProducerIPs.Add(producer, decryptedIp);
                }
                catch
                {
                    _logger.LogWarning($"Unable to decrypt IP from producer: {producer}.");
                }
            }
            return decryptedProducerIPs;
        }
    }
}
