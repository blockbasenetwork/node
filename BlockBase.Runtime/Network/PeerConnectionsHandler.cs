using BlockBase.Domain;
using BlockBase.Domain.Configurations;
using BlockBase.Network;
using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Sidechain;
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
        private bool _incomingConnectionOngoing;

        public PeerConnectionsHandler(INetworkService networkService, SystemConfig systemConfig, ILogger<PeerConnectionsHandler> logger, IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations)
        {
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
            var connectionTasks = new List<Task>();

            foreach (var producerIP in producersIPs)
            {
                connectionTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var peerConnection = AddIfNotExistsPeerConnection(producerIP.Value, producerIP.Key);
                        if (peerConnection.ConnectionState == ConnectionStateEnum.Connected) return;
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
                        _logger.LogError("Couldn't connect to peer.", e);
                    }
                }));
            }

            await Task.WhenAll(connectionTasks);
        }

        public void AddKnownSidechain(SidechainPool sidechain)
        {
            var sidechainAlreadyKnown = KnownSidechains.GetEnumerable().Where(p => p.ClientAccountName == sidechain.ClientAccountName).SingleOrDefault();
            if (sidechainAlreadyKnown != null) KnownSidechains.Replace(sidechainAlreadyKnown, sidechain);
            else KnownSidechains.Add(sidechain);
        }

        public async Task UpdateConnectedProducersInSidechainPool(SidechainPool sidechain)
        {
            var producersInPoolList = sidechain.ProducersInPool.GetEnumerable().ToList();
            var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producersInPoolList, producersInPoolList.FindIndex(m => m.ProducerInfo.AccountName == _nodeConfigurations.AccountName));

            var numberOfConnections = (int)Math.Ceiling(producersInPoolList.Count / 4.0);

            var producersWhoIAmSupposedToBeConnected = orderedProducersInPool.Take(numberOfConnections).Where(m => m.PeerConnection == null || m.PeerConnection.ConnectionState != ConnectionStateEnum.Connected).ToList();
            producersWhoIAmSupposedToBeConnected = producersWhoIAmSupposedToBeConnected.Where(p => !CurrentPeerConnections.GetEnumerable().Any(c => c.IPEndPoint == p.PeerConnection?.IPEndPoint && p.PeerConnection.ConnectionState == ConnectionStateEnum.Connected)).ToList();

            if (producersWhoIAmSupposedToBeConnected.Any()) _logger.LogDebug("Connect to producers in Sidechain: " + sidechain.ClientAccountName);
            foreach (ProducerInPool producer in producersWhoIAmSupposedToBeConnected)
            {
                await ConnectToProducer(sidechain, producer);
            }
        }

        private async Task ConnectToProducer(SidechainPool sidechain, ProducerInPool producer)
        {
            _logger.LogDebug("Connect to Producer: " + producer.ProducerInfo.AccountName);

            if (producer.ProducerInfo.IPEndPoint != null)
            {
                try

                {
                    _tryingConnection = true;


                    producer.PeerConnection = AddIfNotExistsPeerConnection(producer.ProducerInfo.IPEndPoint, producer.ProducerInfo.AccountName);
                    var peerConnected = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.IsEqualTo(producer.ProducerInfo.IPEndPoint)).SingleOrDefault();

                    if (peerConnected == null)
                    {
                        _logger.LogDebug("     Connect to ip: " + producer.ProducerInfo.IPEndPoint.Address + ":" + producer.ProducerInfo.IPEndPoint.Port);
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
                        _waitingForApprovalPeers.Remove(peerConnected);
                        Disconnect(producer.PeerConnection);
                    }
                }
                finally
                {
                    _tryingConnection = false;
                }


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

        private async void TcpConnector_PeerConnected(object sender, PeerConnectedEventArgs args)
        {
            int count = 0;

            while (_tryingConnection || _incomingConnectionOngoing)
            {
                //polite wait
                await Task.Delay(1000);
                count++;
                _logger.LogDebug($"Looping thread id {Task.CurrentId} counter {count}");
                if (count > 2)
                {
                    _tryingConnection = false;
                    _incomingConnectionOngoing = false;
                    break;
                }
                continue;
            }

            _incomingConnectionOngoing = true;

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
                if (!_waitingForApprovalPeers.Contains((p) => p.EndPoint.Address.ToString() == args.Peer.EndPoint.Address.ToString()))
                    _waitingForApprovalPeers.Add(args.Peer);
            }

            _incomingConnectionOngoing = false;
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

            //TODO rpinto noticed that this may fail because it may have multiple equal elements with same endpoint - added contains check before adding element
            var peer = _waitingForApprovalPeers.GetEnumerable().Where(p => p.EndPoint.IsEqualTo(args.IPEndPoint)).SingleOrDefault();
            if (peer != null) _waitingForApprovalPeers.Remove(peer);
        }

        private async void MessageForwarder_IdentificationMessageReceived(IdentificationMessageReceivedEventArgs args)
        {
            int count = 0;
            while (_incomingConnectionOngoing)
            {
                //polite wait
                await Task.Delay(1000);
                count++;
                _logger.LogDebug($"Looping thread id {Task.CurrentId} counter {count}");
                if (count > 2)
                {
                    _incomingConnectionOngoing = false;
                    break;
                }
                continue;
            }

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
                _logger.LogDebug("I do not know this provider/client.");
                Disconnect(peer);
            }

            else if (producer.PeerConnection == null || (producer.PeerConnection.ConnectionState != ConnectionStateEnum.Connected && producer.PeerConnection.Rating > MINIMUM_RATING))
            {
                _logger.LogDebug("Acceptable peer.");
                producer.ProducerInfo.IPEndPoint = peer.EndPoint;
                producer.PeerConnection = AddIfNotExistsPeerConnection(producer.ProducerInfo.IPEndPoint, producer.ProducerInfo.AccountName);
                producer.PeerConnection.ConnectionState = ConnectionStateEnum.Connected;
                producer.PeerConnection.Peer = peer;
            }

            else
            {
                if (producer.PeerConnection.ConnectionState == ConnectionStateEnum.Connected) _logger.LogDebug($"Existing connection found with peer {producer.ProducerInfo.AccountName}");
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
            AddKnownSidechain(sidechain);

            var peersConnected = true;
            if (_checkingConnection) return peersConnected;

            try
            {
                _checkingConnection = true;
                var random = new Random();
                var checks = new List<Task>();

                foreach (var producer in sidechain.ProducersInPool)
                {
                    if (producer.PeerConnection != null && producer.PeerConnection.ConnectionState == ConnectionStateEnum.Connected)
                    {
                        checks.Add(Task.Run(async () =>
                        {
                            var randomInt = random.Next();
                            await SendPingPongMessage(true, producer.PeerConnection.IPEndPoint, randomInt);

                            var pongResponseTask = _networkService.ReceiveMessage(NetworkMessageTypeEnum.Pong);
                            if (pongResponseTask.Wait((int)_networkConfigurations.ConnectionExpirationTimeInSeconds * 1000))
                            {
                                var pongNonce = pongResponseTask.Result?.Result != null ? BitConverter.ToInt32(pongResponseTask.Result.Result.Payload, 0) : random.Next();
                                if (randomInt == pongNonce) return;
                            }

                            _logger.LogDebug($"No response from {producer.ProducerInfo.AccountName}. Removing connection");
                            Disconnect(producer.PeerConnection);
                            peersConnected = false;
                        }));
                    }
                }

                await Task.WhenAll(checks);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Check connection failed with exception: {e}");
            }

            _checkingConnection = false;

            return peersConnected;
        }

        public async Task<List<(bool connectionAlive, PeerConnection peer)>> PingAllConnectionsAndReturnAliveState()
        {
            var random = new Random();
            var peersToReturn = new List<(bool connectionAlive, PeerConnection peer)>();
            var checks = new List<Task>();

            foreach (var peer in CurrentPeerConnections)
            {
                checks.Add(Task.Run(async () =>
                {
                    if (peer.ConnectionState == ConnectionStateEnum.Connected)
                    {
                        var randomInt = random.Next();
                        await SendPingPongMessage(true, peer.IPEndPoint, randomInt);

                        var pongResponseTask = _networkService.ReceiveMessage(NetworkMessageTypeEnum.Pong);
                        if (pongResponseTask.Wait(2000))
                        {
                            var pongNonce = pongResponseTask.Result?.Result != null ? BitConverter.ToInt32(pongResponseTask.Result.Result.Payload, 0) : random.Next();
                            if (randomInt == pongNonce)
                            {
                                peersToReturn.Add((true, peer));
                                return;
                            }
                        }
                    }

                    peersToReturn.Add((false, peer));
                }));
            }

            await Task.WhenAll(checks);

            return peersToReturn;
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
                _logger.LogError("Could not connect to peer: " + ex.Message);
            }

            //tries to see if a connection already exists
            try
            {
                var existingPeerConnection = _networkService.GetPeerIfExists(remoteEndPoint);
                if (existingPeerConnection != null) _logger.LogInformation($"Connection to {remoteEndPoint.ToString()} already established");
                return existingPeerConnection;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to discover existing peer connection: " + ex.Message);
            }

            return null;
        }

        public void Disconnect(PeerConnection peerConnection)
        {
            if (peerConnection.Peer == null) return;
            _logger.LogDebug("Disconnect from peer " + peerConnection.Peer.EndPoint.Address + ":" + peerConnection.Peer.EndPoint.Port + ".");
            _networkService.DisconnectPeer(peerConnection.Peer);
            CurrentPeerConnections.Remove(peerConnection);
        }

        private void Disconnect(Peer peer)
        {
            _logger.LogDebug("Disconnect from peer " + peer.EndPoint.Address + ":" + peer.EndPoint.Port + ".");
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