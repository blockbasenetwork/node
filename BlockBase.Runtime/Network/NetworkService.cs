using BlockBase.Network.Connectors;
using BlockBase.Network.IO;
using BlockBase.Network.Rounting;
using BlockBase.Utils;
using BlockBase.Utils.Operation;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.P2P;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using static BlockBase.Network.Connectors.TcpConnector;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.Rounting.MessageForwarder;

namespace BlockBase.Runtime.Network
{
    public class NetworkService : INetworkService
    {
        private readonly ILogger _logger;

        internal MessageSender MessageSender { get; set; }
        internal MessageReceiver MessageReceiver { get; set; }
        internal MessageForwarder MessageForwarder { get; set; }

        private TcpConnector TcpConnector { get; set; }
        private UdpConnector UdpConnector { get; set; }

        private SystemConfig SystemConfig { get; set; }

        public NetworkService(IServiceProvider serviceProvider, SystemConfig config)
        {
            _logger = serviceProvider.GetService<Logger<INetworkService>>();

            TcpConnector = serviceProvider.GetService<TcpConnector>();
            UdpConnector = serviceProvider.GetService<UdpConnector>();
            MessageSender = serviceProvider.GetService<MessageSender>();
            MessageReceiver = serviceProvider.GetService<MessageReceiver>();
            MessageForwarder = serviceProvider.GetService<MessageForwarder>();
            SystemConfig = config;
        }

        public void Run()
        {
            TcpConnector.Start();
            UdpConnector.Start();
        }

        public async Task<Peer> ConnectAsync(IPEndPoint endpoint)
        {
            return await TcpConnector.ConnectAsync(endpoint, new IPEndPoint(SystemConfig.IPAddress, SystemConfig.TcpPort));
        }

        public async Task SendMessageAsync(NetworkMessage message)
        {
            await MessageSender.SendMessage(message);
        }

        public void DisconnectPeer(Peer peer)
        {
            TcpConnector.Disconnect(peer);
        }

        public void SubscribePeerConnectedEvent(PeerConnectedEventHandler eventHandler)
        {
            TcpConnector.PeerConnected += eventHandler;
        }

        public void SubscribePeerDisconnectedEvent(PeerDisconnectedEventHandler eventHandler)
        {
            TcpConnector.PeerDisconnected += eventHandler;
        }

        public void SubscribeMinedBlockReceivedEvent(MinedBlockReceivedEventHandler eventHandler)
        {
            MessageForwarder.MinedBlockReceived += eventHandler;
        }

        public void SubscribeRecoverBlockReceivedEvent(RecoverBlockReceivedEventHandler eventHandler)
        {
            MessageForwarder.RecoverBlockReceived += eventHandler;
        }

        public void SubscribeIdentificationMessageReceivedEvent(IdentificationMessageReceivedEventHandler eventHandler)
        {
            MessageForwarder.IdentificationMessageReceived += eventHandler;
        }

        public void SubscribeTransactionsReceivedEvent(TransactionsReceivedEventHandler eventHandler)
        {
            MessageForwarder.TransactionsReceived += eventHandler;
        }

        public void SubscribeBlocksRequestReceivedEvent(BlocksRequestReceivedEventHandler eventHandler)
        {
            MessageForwarder.BlocksRequestReceived += eventHandler;
        }
        public void SubscribeLastIncludedTransactionRequestReceivedEvent(LastIncludedTransactionRequestReceivedEventHandler eventHandler)
        { 
            MessageForwarder.LastIncludedTransactionRequestReceived += eventHandler;
        }
        public void SubscribePingReceivedEvent(PingReceivedEventHandler eventHandler)
        {
            MessageForwarder.PingReceived += eventHandler;
        }

        public void SubscribeTransactionConfirmationReceivedEvent(TransactionConfirmationReceivedEventHandler eventHandler)
        {
            MessageForwarder.TransactionConfirmationReceived += eventHandler;
        }

        public async Task<OpResult<NetworkMessage>> ReceiveMessage(NetworkMessageTypeEnum type)
        {
            bool messageValidator(NetworkMessage networkMessage)
            {
                return true;
            }

            return await MessageForwarder.RegisterMessageEvent(type, messageValidator, null);
        }

        public void Dispose()
        {
            TcpConnector.DisconnectAllPeers();
        }
    }
}