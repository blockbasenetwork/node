using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Network;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Sidechain;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using BlockBase.Utils.Operation;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.PeerConnection;
using static BlockBase.Network.Rounting.MessageForwarder;

namespace BlockBase.Runtime.Network
{
    public class PeerConnectionsHandler
    {
        //TODO: marciak - UpdatePeerConnectionRating, should they win back reputation with time?
        private readonly SidechainKeeper _sidechainKeeper;
        private readonly INetworkService _networkService;
        private ThreadSafeList<PeerConnection> _currentPeerConnections;
        private ThreadSafeList<Peer> _waitingForApprovalPeers;
        private NodeConfigurations _nodeConfigurations;
        private SystemConfig _systemConfig;
        private ILogger _logger;
        private NetworkConfigurations _networkConfigurations;

        private string _endPoint;

        //TODO: this will not be a constant, it will vary with the number of producers in pool
        private const int MINIMUM_RATING = 1;
        private const int STARTING_RATING = 100;
        private const int RATING_LOST_FOR_DISCONECT = 10;
        private const int RATING_LOST_FOR_CONNECT_FAILURE = 10;
        
        public PeerConnectionsHandler(INetworkService networkService, SidechainKeeper sidechainKeeper, SystemConfig systemConfig, ILogger<PeerConnectionsHandler> logger, IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations)
        {
            _sidechainKeeper = sidechainKeeper;
            _networkService = networkService;
            _systemConfig = systemConfig;
            _logger = logger;
            _networkConfigurations = networkConfigurations?.Value;

            _nodeConfigurations = nodeConfigurations?.Value;
            _currentPeerConnections = new ThreadSafeList<PeerConnection>();
            _waitingForApprovalPeers = new ThreadSafeList<Peer>();

            _networkService.SubscribePeerConnectedEvent(TcpConnector_PeerConnected);
            _networkService.SubscribePeerDisconnectedEvent(TcpConnector_PeerDisconnected);
            _networkService.SubscribeIdentificationMessageReceivedEvent(MessageForwarder_IdentificationMessageReceived);

            _endPoint = systemConfig.IPAddress + ":" + systemConfig.TcpPort;
        }

        #region Enter Points
        public async Task UpdateConnectedProducersInSidechainPool(SidechainPool sidechain)
        {
            _logger.LogDebug("Connect to producers in Sidechain: " + sidechain.SidechainName);
            var producersInPoolList = sidechain.ProducersInPool.GetEnumerable().ToList();
            var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName));

            var numberOfConnections = (int) Math.Ceiling(producersInPoolList.Count/4.0);
            
            var producersWhoIAmSupposedToBeConnected = orderedProducersInPool.Where(m => IsPeerConnectionValid(m)).Take(numberOfConnections).Where(m => m.PeerConnection == null || m.PeerConnection.ConnectionState != ConnectionStateEnum.Connected).ToList();

            foreach (ProducerInPool producer in producersWhoIAmSupposedToBeConnected)
            {
                await ConnectToProducer(sidechain, producer);
            }
        }

        public void RemovePoolConnections(SidechainPool sidechain)
        {
            foreach (ProducerInPool producerInPool in sidechain.ProducersInPool)
            {
                RemoveProducerConnectionIfPossible(producerInPool);
            }
        }

        public void RemoveProducerConnectionIfPossible(ProducerInPool producerInPool)
        {
            if (producerInPool.PeerConnection != null && producerInPool.PeerConnection.ConnectionState == ConnectionStateEnum.Connected)
            {
                if (CanDeleteConnection(producerInPool.PeerConnection))
                {
                    Disconnect(producerInPool.PeerConnection.Peer);
                }
            }
        }

        private async Task ConnectToProducer(SidechainPool sidechain, ProducerInPool producer)
        {
            _logger.LogDebug("Connect to Producer: " + producer.ProducerInfo.AccountName);

            if (producer.ProducerInfo.IPEndPoint != null)
            { 
                producer.PeerConnection = AddIfNotExistsPeerConnection(producer.ProducerInfo);
                var peerConnected = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.Equals(producer.ProducerInfo.IPEndPoint)).SingleOrDefault();

                if (peerConnected == null)
                {
                    _logger.LogInformation("     Connect to ip: " + producer.ProducerInfo.IPEndPoint.Address + ":" + producer.ProducerInfo.IPEndPoint.Port);
                    var peer = await ConnectAsync(producer.ProducerInfo.IPEndPoint, new IPEndPoint(_systemConfig.IPAddress, _systemConfig.TcpPort));
                    if (peer == null)
                    {
                        await UpdatePeerConnectionRating(producer.PeerConnection, RATING_LOST_FOR_CONNECT_FAILURE);
                    }
                    else
                    {
                        await SendIdentificationMessage(producer.ProducerInfo.IPEndPoint);
                    }
                }
            }
            else
            {
                if (!producer.ProducerInfo.NewlyJoined)
                {
                    _logger.LogDebug("     Asking for IP.");
                    var connectedProducers = sidechain.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection.ConnectionState == ConnectionStateEnum.Connected);
                    foreach (ProducerInPool m in connectedProducers)
                    {
                        await AskForKnownPeer(producer.ProducerInfo, m.PeerConnection.IPEndPoint.Address, m.PeerConnection.IPEndPoint.Port);
                    }
                }
            }
        }

        private async void TcpConnector_PeerDisconnected(object sender, PeerDisconnectedEventArgs args)
        {
            var peerConnection = _currentPeerConnections.GetEnumerable().Where(p => p.IPEndPoint.Equals(args.IPEndPoint)).SingleOrDefault();
            if (peerConnection != null)
            {
                var i = 0;
                while (i < _networkConfigurations.MaxNumberOfConnectionRetries)
                {
                    i++;
                    var peerConnectionRetry = await ConnectAsync(args.IPEndPoint, new IPEndPoint(_systemConfig.IPAddress, _systemConfig.TcpPort));

                    if (peerConnectionRetry == null) 
                        continue;
                    else 
                        return;
                }
                peerConnection.ConnectionState = ConnectionStateEnum.Disconnected;
                await UpdatePeerConnectionRating(peerConnection, RATING_LOST_FOR_DISCONECT);
                _logger.LogDebug("Peer Connections handler :: Removing peer connection.");
                _currentPeerConnections.Remove(peerConnection);
            }

            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.Equals(args.IPEndPoint)).SingleOrDefault();
            if (peer != null) _waitingForApprovalPeers.Remove(peer);
        }

        private void TcpConnector_PeerConnected(object sender, PeerConnectedEventArgs args)
        {
            var peerConnection = _currentPeerConnections.GetEnumerable().Where(p => p.IPEndPoint.Equals(args.Peer.EndPoint)).SingleOrDefault();
            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.Equals(args.Peer.EndPoint)).SingleOrDefault();

            if (peerConnection != null)
            {
                if (peerConnection.Rating < MINIMUM_RATING)
                {
                    _logger.LogDebug($"Peer rating ({peerConnection.Rating}) below minimum.");
                    Disconnect(args.Peer);
                }
                else
                {
                    peerConnection.ConnectionState = ConnectionStateEnum.Connected;
                    peerConnection.Peer = args.Peer;
                }
            }

            else if (peer == null)
            {
                _waitingForApprovalPeers.Add(args.Peer);
            }
        }

        private void MessageForwarder_IdentificationMessageReceived(IdentificationMessageReceivedEventArgs args)
        {
            var producer = _sidechainKeeper.Sidechains.Values.SelectMany(p => p.ProducersInPool.GetEnumerable().Where(m => m.ProducerInfo.AccountName == args.EosAccount && m.ProducerInfo.PublicKey == args.PublicKey)).FirstOrDefault();

            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.Equals(args.SenderIPEndPoint)).SingleOrDefault();
            if (peer == null) {
                _logger.LogDebug("There's no peer with this ip waiting for confirmation.");
                return;
            }

            if (producer == null)
            {
                _logger.LogDebug("I do not know this producer.");
                Disconnect(peer);
            }

            else if (producer.PeerConnection == null || (producer.PeerConnection.ConnectionState != ConnectionStateEnum.Connected && producer.PeerConnection.Rating > MINIMUM_RATING))
            {
                _logger.LogDebug("Acceptable producer.");
                producer.ProducerInfo.IPEndPoint = peer.EndPoint;
                producer.PeerConnection = AddIfNotExistsPeerConnection(producer.ProducerInfo);
                producer.PeerConnection.ConnectionState = ConnectionStateEnum.Connected;
                producer.PeerConnection.Peer = peer;
            }

            else
            {
                if (producer.PeerConnection.ConnectionState == ConnectionStateEnum.Connected) _logger.LogDebug("I already have another connection with this producer");
                else if (producer.PeerConnection.Rating > MINIMUM_RATING) _logger.LogDebug("Other producer connection reached below the rating threshold.");
                Disconnect(peer);
            }
            _waitingForApprovalPeers.Remove(peer);
        }

        public async Task UpdatePeerConnectionRating(PeerConnection peerConnection, int ratingLost)
        {
            peerConnection.Rating -= ratingLost;
            if (peerConnection.Rating < MINIMUM_RATING)
            {
                var pools = _sidechainKeeper.Sidechains.Where(p => p.Value.ProducersInPool.GetEnumerable().Count(m => m.PeerConnection == peerConnection) != 0);
                Disconnect(peerConnection.Peer);
                foreach (KeyValuePair<string, SidechainPool> keyValuePair in pools)
                {
                    await UpdateConnectedProducersInSidechainPool(keyValuePair.Value);
                }
            }
        }

        public async Task TryReconnectWithDisconnectedAccounts(SidechainPool sidechain)
        {
            foreach(var producer in sidechain.ProducersInPool)
            {
                if(producer.PeerConnection != null && producer.PeerConnection.ConnectionState == ConnectionStateEnum.Disconnected)
                {
                    await ConnectToProducer(sidechain, producer);
                }
            }
        }

        #endregion Enter Points

        #region Auxiliar Methods

        private async Task AskForKnownPeer(ProducerInfo producerInfo, IPAddress ipAddress, int tcpPort)
        {
            var payload = Encoding.ASCII.GetBytes("askforip " + producerInfo.PublicKey);
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendBlockHeaders, payload, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, new IPEndPoint(ipAddress, tcpPort));
            await _networkService.SendMessageAsync(message);
        }

        //rpinto - consider not using this method at all -> marciak - method removed
        //private PeerConnection ChangeConnectionState(PeerConnection connection, ConnectionStateEnum connectionState, Peer peer)

        private PeerConnection AddIfNotExistsPeerConnection(ProducerInfo producerInfo)
        {
            PeerConnection producerPeerConnection = null;

            if (producerInfo.IPEndPoint != null)
            {
                producerPeerConnection = _currentPeerConnections.GetEnumerable().SingleOrDefault(p => p.IPEndPoint.Equals(producerInfo.IPEndPoint));
                if (producerPeerConnection == null)
                {
                    producerPeerConnection = new PeerConnection
                    {
                        ConnectionState = ConnectionStateEnum.Disconnected,
                        Rating = STARTING_RATING,
                        IPEndPoint = producerInfo.IPEndPoint
                    };
                    _currentPeerConnections.Add(producerPeerConnection);
                }
            }
            return producerPeerConnection;
        }

        private IPEndPoint TryParseIpEndPoint(string ipAddressStr, string portStr)
        {
            if (!IPAddress.TryParse(ipAddressStr, out var ipAddress)) return null;
            if (!int.TryParse(portStr, out var port)) return null;

            return new IPEndPoint(ipAddress, port);
        }

        private async Task SendIdentificationMessage(IPEndPoint destinationEndPoint)
        {
            byte[] payload = new byte[0];
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendProducerIdentification, payload, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, destinationEndPoint);
            await _networkService.SendMessageAsync(message);
            _logger.LogDebug("Identification message sent.");
        }

        private async Task<Peer> ConnectAsync(IPEndPoint remoteEndPoint, EndPoint localEndPoint = null)
        {
            try
            {
                var peer = await _networkService.ConnectAsync(remoteEndPoint);
                return peer;
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not connect to producer: " + ex.Message);
                return null;
            }
        }

        private void Disconnect(Peer peer)
        {
            _logger.LogInformation("Disconnect from peer " + peer.EndPoint.Address + ":" + peer.EndPoint.Port + ".");
            _networkService.DisconnectPeer(peer);
        }

        private bool CanDeleteConnection(PeerConnection peerConnection)
        {
            var numberOfSidechainsWherePeerConnectionExists = _sidechainKeeper.Sidechains.Values.Count(s => s.ProducersInPool.GetEnumerable().Count(m => m.PeerConnection != null && m.PeerConnection.IPEndPoint.Equals(peerConnection.IPEndPoint)) != 0);
            if (numberOfSidechainsWherePeerConnectionExists > 1)
            {
                return false;
            }
            return true;
        }

        public bool IsPeerConnectionValid(ProducerInPool producer)
        {
            if (producer.PeerConnection != null && producer.PeerConnection.Rating < MINIMUM_RATING)
            {
                return false;
            }
            return true;
        }

        #endregion Auxiliar Methods
    }
}