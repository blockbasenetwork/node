using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Network;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.SidechainProducer;
using BlockBase.Utils;
using BlockBase.Utils.Extensions;
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
        //TODO: Remove
        private readonly SidechainKeeper _sidechainKeeper;
        private readonly INetworkService _networkService;
        public ThreadSafeList<SidechainPool> KnownSidechains { get; set; }
        public ThreadSafeList<PeerConnection> CurrentPeerConnections { private set; get; }
        private ThreadSafeList<Peer> _waitingForApprovalPeers;
        private NodeConfigurations _nodeConfigurations;
        private SystemConfig _systemConfig;
        private ILogger _logger;
        private NetworkConfigurations _networkConfigurations;

        private string _endPoint;
        private bool _checkingConnection;

        //TODO: this will not be a constant, it will vary with the number of producers in pool
        private const int MINIMUM_RATING = 1;
        private const int STARTING_RATING = 100;
        private const int RATING_LOST_FOR_DISCONECT = 10;
        private const int RATING_LOST_FOR_CONNECT_FAILURE = 10;
        private bool _tryingConnection;

        public PeerConnectionsHandler(INetworkService networkService, SidechainKeeper sidechainKeeper, SystemConfig systemConfig, ILogger<PeerConnectionsHandler> logger, IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations)
        {
            _sidechainKeeper = sidechainKeeper;
            _networkService = networkService;
            _systemConfig = systemConfig;
            _logger = logger;
            _networkConfigurations = networkConfigurations?.Value;

            _nodeConfigurations = nodeConfigurations?.Value;

            CurrentPeerConnections = new ThreadSafeList<PeerConnection>();
            KnownSidechains = new ThreadSafeList<SidechainPool>();
            _waitingForApprovalPeers = new ThreadSafeList<Peer>();

            _networkService.SubscribePeerConnectedEvent(TcpConnector_PeerConnected);
            _networkService.SubscribePeerDisconnectedEvent(TcpConnector_PeerDisconnected);
            _networkService.SubscribeIdentificationMessageReceivedEvent(MessageForwarder_IdentificationMessageReceived);
            _networkService.SubscribePingReceivedEvent(MessageForwarder_PingMessageReceived);

            _endPoint = systemConfig.IPAddress + ":" + systemConfig.TcpPort;
        }

        #region Enter Points

        public async Task ConnectToProducers(IDictionary<string, IPEndPoint> producersIPs)
        {
            foreach (var producerIP in producersIPs)
            {
                try
                {
                    var peerConnection = AddIfNotExistsPeerConnection(producerIP.Value, producerIP.Key);
                    if (peerConnection.ConnectionState == ConnectionStateEnum.Connected) continue;
                    var peer = await ConnectAsync(producerIP.Value);
                    if (peer != null)
                    {
                        peerConnection.ConnectionState = ConnectionStateEnum.Connected;
                        await SendIdentificationMessage(producerIP.Value);
                    }
                    else
                    {
                        CurrentPeerConnections.Remove(peerConnection);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Couldn't connect to producer.", e);
                }
            }
        }

        public async Task UpdateConnectedProducersInSidechainPool(SidechainPool sidechain)
        {
            var sidechainAlreadyKnown = KnownSidechains.GetEnumerable().Where(p => p.ClientAccountName == sidechain.ClientAccountName).SingleOrDefault();
            if (sidechainAlreadyKnown != null) KnownSidechains.Remove(sidechainAlreadyKnown);
            
            KnownSidechains.Add(sidechain);

            var producersInPoolList = sidechain.ProducersInPool.GetEnumerable().ToList();
            var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName));

            var numberOfConnections = (int)Math.Ceiling(producersInPoolList.Count / 4.0);

            var producersWhoIAmSupposedToBeConnected = orderedProducersInPool.Take(numberOfConnections).Where(m => m.PeerConnection == null || m.PeerConnection.ConnectionState != ConnectionStateEnum.Connected).ToList();
            producersWhoIAmSupposedToBeConnected = producersWhoIAmSupposedToBeConnected.Where(p => !CurrentPeerConnections.GetEnumerable().Any(c => c.IPEndPoint == p.PeerConnection?.IPEndPoint)).ToList();

            if (producersWhoIAmSupposedToBeConnected.Any()) _logger.LogDebug("Connect to producers in Sidechain: " + sidechain.ClientAccountName);
            foreach (ProducerInPool producer in producersWhoIAmSupposedToBeConnected)
            {
                await ConnectToProducer(sidechain, producer);
            }
        }

        public void RemovePoolConnections(SidechainPool sidechain)
        {

            foreach (ProducerInPool producerInPool in sidechain.ProducersInPool)
            {
                TryToRemoveConnection(producerInPool.PeerConnection);
            }

            var clientConnection = CurrentPeerConnections.GetEnumerable().Where(c => c.ConnectionAccountName == sidechain.ClientAccountName).SingleOrDefault();
            TryToRemoveConnection(clientConnection);
        }

        public void TryToRemoveConnection(PeerConnection peerConnection)
        {
            if (peerConnection != null && peerConnection.ConnectionState == ConnectionStateEnum.Connected)
            {
                if (CanDeleteConnection(peerConnection))
                {
                    Disconnect(peerConnection);
                }
            }
        }

        private async Task ConnectToProducer(SidechainPool sidechain, ProducerInPool producer)
        {
            _logger.LogDebug("Connect to Producer: " + producer.ProducerInfo.AccountName);

            if (producer.ProducerInfo.IPEndPoint != null)
            {
                _tryingConnection = true;
                producer.PeerConnection = AddIfNotExistsPeerConnection(producer.ProducerInfo.IPEndPoint, producer.ProducerInfo.AccountName);
                var peerConnected = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.IsEqualTo(producer.ProducerInfo.IPEndPoint)).SingleOrDefault();

                if (peerConnected == null)
                {
                    _logger.LogInformation("     Connect to ip: " + producer.ProducerInfo.IPEndPoint.Address + ":" + producer.ProducerInfo.IPEndPoint.Port);
                    var peer = await ConnectAsync(producer.ProducerInfo.IPEndPoint, new IPEndPoint(_systemConfig.IPAddress, _systemConfig.TcpPort));
                    if (peer != null)
                    {
                        await SendIdentificationMessage(producer.ProducerInfo.IPEndPoint);
                    }
                    else
                    {
                        CurrentPeerConnections.Remove(producer.PeerConnection);
                    }
                }
                else
                {
                    Disconnect(producer.PeerConnection);
                }
                _tryingConnection = false;
            }
            else
            {
                if (!producer.ProducerInfo.NewlyJoined)
                {
                    _logger.LogDebug("     Asking for IP.");
                    var connectedProducers = sidechain.ProducersInPool.GetEnumerable().Where(m => m.PeerConnection?.ConnectionState == ConnectionStateEnum.Connected);
                    foreach (ProducerInPool m in connectedProducers)
                    {
                        await AskForKnownPeer(producer.ProducerInfo, m.PeerConnection.IPEndPoint.Address, m.PeerConnection.IPEndPoint.Port);
                    }
                }
            }
        }

        private void TcpConnector_PeerDisconnected(object sender, PeerDisconnectedEventArgs args)
        {
            var peerConnection = CurrentPeerConnections.GetEnumerable().Where(p => p.IPEndPoint.IsEqualTo(args.IPEndPoint)).SingleOrDefault();
            if (peerConnection != null)
            {
                peerConnection.ConnectionState = ConnectionStateEnum.Disconnected;
                _logger.LogDebug("Peer Connections handler :: Removing peer connection.");
                CurrentPeerConnections.Remove(peerConnection);
            }

            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.IsEqualTo(args.IPEndPoint)).SingleOrDefault();
            if (peer != null) _waitingForApprovalPeers.Remove(peer);
        }

        private void TcpConnector_PeerConnected(object sender, PeerConnectedEventArgs args)
        {
            while (_tryingConnection)
            {
                continue;
            }
            var peerConnection = CurrentPeerConnections.GetEnumerable().Where(p => p.IPEndPoint.IsEqualTo(args.Peer.EndPoint)).SingleOrDefault();
            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.IsEqualTo(args.Peer.EndPoint)).SingleOrDefault();

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
            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.IsEqualTo(args.SenderIPEndPoint)).SingleOrDefault();
            if (peer == null)
            {
                _logger.LogDebug("There's no peer with this ip waiting for confirmation.");
                return;
            }

            var sidechainPool = KnownSidechains.GetEnumerable().Where(s => s.ClientAccountName == args.EosAccount).SingleOrDefault();
            if (sidechainPool != null)
            {
                _logger.LogDebug("Acceptable client connection.");
                var peerConnection = AddIfNotExistsPeerConnection(args.SenderIPEndPoint, sidechainPool.ClientAccountName);
                peerConnection.ConnectionState = ConnectionStateEnum.Connected;
                peerConnection.Peer = peer;
                _waitingForApprovalPeers.Remove(peer);
                return;
            }

            var producer = KnownSidechains.GetEnumerable().SelectMany(p => p.ProducersInPool.GetEnumerable().Where(m => m.ProducerInfo.AccountName == args.EosAccount)).FirstOrDefault();

            if (producer == null)
            {
                _logger.LogDebug("I do not know this producer.");
                Disconnect(peer);
            }

            else if (producer.PeerConnection == null || (producer.PeerConnection.ConnectionState != ConnectionStateEnum.Connected && producer.PeerConnection.Rating > MINIMUM_RATING))
            {
                _logger.LogDebug("Acceptable producer.");
                producer.ProducerInfo.IPEndPoint = peer.EndPoint;
                producer.PeerConnection = AddIfNotExistsPeerConnection(producer.ProducerInfo.IPEndPoint, producer.ProducerInfo.AccountName);
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

        private async void MessageForwarder_PingMessageReceived(PingReceivedEventArgs args, IPEndPoint sender)
        {
            await SendPingPongMessage(false, sender, args.nonce);
        }

        public async Task<bool> ArePeersConnected(SidechainPool sidechain)
        {
            var peersConnected = true;
            if (_checkingConnection) return peersConnected;
            
            try
            {
                _checkingConnection = true;
                var random = new Random();

                foreach (var producer in sidechain.ProducersInPool)
                {
                    if (producer.PeerConnection != null && producer.PeerConnection.ConnectionState == ConnectionStateEnum.Connected)
                    {
                        var randomInt = random.Next();
                        await SendPingPongMessage(true, producer.PeerConnection.IPEndPoint, randomInt);

                        var pongResponseTask = _networkService.ReceiveMessage(NetworkMessageTypeEnum.Pong);
                        if (pongResponseTask.Wait((int)_networkConfigurations.ConnectionExpirationTimeInSeconds * 1000))
                        {
                            var pongNonce = pongResponseTask.Result?.Result != null ? BitConverter.ToInt32(pongResponseTask.Result.Result.Payload, 0) : random.Next();
                            if (randomInt == pongNonce) continue;
                        }

                        _logger.LogDebug($"No response from {producer.ProducerInfo.AccountName}. Removing connection");
                        Disconnect(producer.PeerConnection);
                        peersConnected = false;
                    }
                }
                _checkingConnection = false;
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Check connection failed with exception: {e}");
            }

            return peersConnected;
        }

        #endregion Enter Points

        #region Auxiliar Methods

        private async Task AskForKnownPeer(ProducerInfo producerInfo, IPAddress ipAddress, int tcpPort)
        {
            var payload = Encoding.ASCII.GetBytes("askforip " + producerInfo.PublicKey);
            var message = new NetworkMessage(NetworkMessageTypeEnum.SendBlockHeaders, payload, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, new IPEndPoint(ipAddress, tcpPort));
            await _networkService.SendMessageAsync(message);
        }

        private PeerConnection AddIfNotExistsPeerConnection(IPEndPoint ipEndPoint, string accountName)
        {
            PeerConnection peerConnection = null;

            if (ipEndPoint != null)
            {
                peerConnection = CurrentPeerConnections.GetEnumerable().SingleOrDefault(p => p.IPEndPoint.IsEqualTo(ipEndPoint) || p.ConnectionAccountName == accountName);
                if (peerConnection == null)
                {
                    peerConnection = new PeerConnection
                    {
                        ConnectionState = ConnectionStateEnum.Disconnected,
                        Rating = STARTING_RATING,
                        IPEndPoint = ipEndPoint,
                        ConnectionAccountName = accountName

                    };
                    CurrentPeerConnections.Add(peerConnection);
                }
            }
            return peerConnection;
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

        private async Task SendPingPongMessage(bool isPingMessage, IPEndPoint destinationEndPoint, int nonce)
        {
            byte[] payload = BitConverter.GetBytes(nonce);
            var type = isPingMessage ? NetworkMessageTypeEnum.Ping : NetworkMessageTypeEnum.Pong;
            var message = new NetworkMessage(type, payload, TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _endPoint, _nodeConfigurations.AccountName, destinationEndPoint);
            await _networkService.SendMessageAsync(message);
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

        private void Disconnect(PeerConnection peerConnection)
        {
            if (peerConnection.Peer == null) return;
            _logger.LogInformation("Disconnect from peer " + peerConnection.Peer.EndPoint.Address + ":" + peerConnection.Peer.EndPoint.Port + ".");
            _networkService.DisconnectPeer(peerConnection.Peer);
            CurrentPeerConnections.Remove(peerConnection);
        }

        private void Disconnect(Peer peer)
        {
            _logger.LogInformation("Disconnect from peer " + peer.EndPoint.Address + ":" + peer.EndPoint.Port + ".");
            _networkService.DisconnectPeer(peer);
        }

        private bool CanDeleteConnection(PeerConnection peerConnection)
        {
            var numberOfSidechainsWherePeerConnectionExists = KnownSidechains.GetEnumerable().Count(s => s.ProducersInPool.GetEnumerable().Count(m => m.PeerConnection != null && m.PeerConnection.IPEndPoint.IsEqualTo(peerConnection.IPEndPoint)) != 0);
            if (numberOfSidechainsWherePeerConnectionExists > 1)
            {
                return false;
            }
            return true;
        }

        #endregion Auxiliar Methods
    }
}