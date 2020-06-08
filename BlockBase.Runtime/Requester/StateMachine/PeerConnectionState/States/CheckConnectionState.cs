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

namespace BlockBase.Runtime.Requester.StateMachine.PeerConnectionsState.States
{
    public class CheckConnectionState : AbstractState<StartState, EndState>
    {
        private readonly IMainchainService _mainchainService;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private bool _peersConnected;
        private SidechainPool _sidechainPool;
        private ContractStateTable _contractStateTable;
        public CheckConnectionState(SidechainPool sidechain, ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, PeerConnectionsHandler peerConnectionsHandler): base(logger)
        {
            _mainchainService = mainchainService;
            _sidechainPool = sidechain;
            _peerConnectionsHandler = peerConnectionsHandler;
            _peersConnected = true;
        }

        protected override Task<bool> IsWorkDone()
        {
            throw new NotImplementedException();
        }

        protected override async Task DoWork()
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> HasConditionsToContinue()
        {
            throw new NotImplementedException();
        }

        protected override Task<(bool inConditionsToJump, string nextState)> HasConditionsToJump()
        {
            throw new NotImplementedException();
        }

        protected override async Task UpdateStatus() 
        {
            throw new NotImplementedException();
        }

        // private async Task CheckPeerConnections(List<ProducerInTable> producers)
        // {
        //     var currentConnections = _peerConnectionsHandler.CurrentPeerConnections.GetEnumerable();

        //     //TODO rpinto - commented this fetch to pass as parameter but I'm not sure it needs to be refreshed from before
        //     // var producers = await _mainchainService.RetrieveProducersFromTable(_sidechain.ClientAccountName);
        //     var producersInPool = producers.Select(m => new ProducerInPool
        //     {
        //         ProducerInfo = new ProducerInfo
        //         {
        //             AccountName = m.Key,
        //             PublicKey = m.PublicKey,
        //             ProducerType = (ProducerTypeEnum)m.ProducerType,
        //             NewlyJoined = false,
        //             IPEndPoint = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()?.IPEndPoint
        //         },
        //         PeerConnection = currentConnections.Where(p => p.ConnectionAccountName == m.Key).FirstOrDefault()
        //     }).ToList();

        //     _sidechain.ProducersInPool.ClearAndAddRange(producersInPool);

        //     //TODO rpinto - this may also take time but is awaited. Why this way here and different right below
        //     await ConnectToProducers();

        //     if (_sidechain.ProducersInPool.GetEnumerable().Any(p => p.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected))
        //     {
        //         //TODO rpinto - this returns a TaskContainer that isn't stored anywhere. So this is executed and not awaited. Is that the intended behavior?
        //         var checkConnectionTask = TaskContainer.Create(async () => await _peerConnectionsHandler.ArePeersConnected(_sidechain));
        //         checkConnectionTask.Start();
        //     }
        // }
    }
}