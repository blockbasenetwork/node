using BlockBase.Network.IO;
using BlockBase.Utils.Operation;
using Open.P2P;
using System;
using System.Net;
using System.Threading.Tasks;
using static BlockBase.Network.Connectors.TcpConnector;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using static BlockBase.Network.Rounting.MessageForwarder;

namespace BlockBase.Runtime.Network
{
    public interface INetworkService : IDisposable
    {
        Task<Peer> ConnectAsync(IPEndPoint endpoint);
        Task SendMessageAsync(NetworkMessage message);
        Task<OpResult<NetworkMessage>> ReceiveMessage(NetworkMessageTypeEnum type);

        void DisconnectPeer(Peer peer);

        void Run();

        void SubscribePeerConnectedEvent(PeerConnectedEventHandler eventHandler);
        void SubscribePeerDisconnectedEvent(PeerDisconnectedEventHandler eventHandler);
        void SubscribeMinedBlockReceivedEvent(MinedBlockReceivedEventHandler eventHandler);
        void SubscribeRecoverBlockReceivedEvent(RecoverBlockReceivedEventHandler eventHandler);
        void SubscribeIdentificationMessageReceivedEvent(IdentificationMessageReceivedEventHandler eventHandler);
        void SubscribeTransactionsReceivedEvent(TransactionsReceivedEventHandler eventHandler);
        void SubscribeBlocksRequestReceivedEvent(BlocksRequestReceivedEventHandler eventHandler);   
        void SubscribePingReceivedEvent(PingReceivedEventHandler eventHandler);
        void SubscribePongReceivedEvent(PongReceivedEventHandler eventHandler);
        void SubscribeTransactionConfirmationReceivedEvent(TransactionConfirmationReceivedEventHandler eventHandler);

        void UnSubscribePeerConnectedEvent(PeerConnectedEventHandler eventHandler);
        void UnSubscribePeerDisconnectedEvent(PeerDisconnectedEventHandler eventHandler);
        void UnSubscribeMinedBlockReceivedEvent(MinedBlockReceivedEventHandler eventHandler);
        void UnSubscribeRecoverBlockReceivedEvent(RecoverBlockReceivedEventHandler eventHandler);
        void UnSubscribeIdentificationMessageReceivedEvent(IdentificationMessageReceivedEventHandler eventHandler);
        void UnSubscribeTransactionsReceivedEvent(TransactionsReceivedEventHandler eventHandler);
        void UnSubscribeBlocksRequestReceivedEvent(BlocksRequestReceivedEventHandler eventHandler);   
        void UnSubscribePingReceivedEvent(PingReceivedEventHandler eventHandler);
        void UnSubscribePongReceivedEvent(PongReceivedEventHandler eventHandler);
        void UnSubscribeTransactionConfirmationReceivedEvent(TransactionConfirmationReceivedEventHandler eventHandler);
    }
}