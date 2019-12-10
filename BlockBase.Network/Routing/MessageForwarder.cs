using BlockBase.Domain.Protos;
using BlockBase.Network.Exceptions;
using BlockBase.Network.IO;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Operation;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Open.P2P;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;

namespace BlockBase.Network.Rounting
{
    public class MessageForwarder
    {
        public ThreadSafeList<MessageEvent> MessageEvents { get; set; }

        public ThreadSafeList<NetworkMessage> NetworkMessages { get; set; }

        private readonly ILogger _logger;

        public MessageForwarder(ILogger<MessageForwarder> logger)
        {
            MessageEvents = new ThreadSafeList<MessageEvent>();
            NetworkMessages = new ThreadSafeList<NetworkMessage>();

            _logger = logger;
        }

        public void ReleaseAllExpiredMessageEvents()
        {
            foreach (var messageEvent in MessageEvents.GetEnumerable())
                if (messageEvent.Timeout.HasValue && messageEvent.Timeout < DateTime.UtcNow)
                {
                    MessageEvents.Remove(messageEvent);
                    messageEvent.Awaitable.SetCanceled();
                }
        }

        public async Task<OpResult<NetworkMessage>> RegisterMessageEvent(NetworkMessageTypeEnum messageType, Func<NetworkMessage, bool> messageValidator, Func<NetworkMessage, bool> exceptionCases = null, DateTime? timeout = null)
        {
            var messageEvent = new MessageEvent(timeout);
            //_logger.LogDebug($"RegisterMessageEvent. {messageType}");
            messageEvent.MessageType = messageType;
            messageEvent.MessageValidator = messageValidator;
            messageEvent.ExceptionCases = exceptionCases;

            foreach (var networkMessage in NetworkMessages.GetEnumerable())
            {
                if (networkMessage.NetworkMessageType == messageType && messageValidator(networkMessage))
                {
                    NetworkMessages.Remove(networkMessage);
                    return new OpResult<NetworkMessage>(networkMessage);
                }

                if (messageEvent != null && messageEvent.ExceptionCases != null && messageEvent.ExceptionCases(networkMessage))
                {
                    return new OpResult<NetworkMessage>(new NetworkException(networkMessage));
                }
            }


            MessageEvents.Add(messageEvent);

            return await messageEvent.Awaitable.Task;
        }

        //TODO this method needs revision for multithreading and high performance
        public void ProcessReceivedMessage(NetworkMessage message)
        {
            //_logger.LogDebug($"ProcessReceivedMessage. {message.NetworkMessageType} Sent by: {message.Sender}");
            bool messageProcessed = false;
            foreach (var m in MessageEvents.GetEnumerable())
            {
                if (m.ExceptionCases != null && m.ExceptionCases(message))
                {
                    m.Awaitable.SetResult(new OpResult<NetworkMessage>(new NetworkException(message)));
                }

                if (m.MessageType == NetworkMessageTypeEnum.Unknown || m.MessageType == message.NetworkMessageType && m.MessageValidator(message))
                {

                    if (!m.Awaitable.Task.IsCompleted) m.Awaitable.SetResult(new OpResult<NetworkMessage>(message));
                    MessageEvents.Remove(m);
                    messageProcessed = true;
                }
            }

            if (!messageProcessed)
            {
                //NetworkMessages.Add(message);

                if (message.NetworkMessageType == NetworkMessageTypeEnum.SendMinedBlock) MinedBlockReceived?.Invoke(ParseMinedBlockMessage(message.Payload));

                else if (message.NetworkMessageType == NetworkMessageTypeEnum.SendBlock) RecoverBlockReceived?.Invoke(ParseMinedBlockMessage(message.Payload), message.Sender);

                else if(message.NetworkMessageType == NetworkMessageTypeEnum.SendProducerIdentification)IdentificationMessageReceived?.Invoke(new IdentificationMessageReceivedEventArgs {PublicKey = message.PublicKey, EosAccount = message.EosAccount, SenderIPEndPoint = message.Sender});

                else if(message.NetworkMessageType == NetworkMessageTypeEnum.RequestBlocks) BlocksRequestReceived?.Invoke(ParseRequestBlocksMessage(message.Payload, message.Sender));

                else if(message.NetworkMessageType == NetworkMessageTypeEnum.SendTransaction) TransactionReceived?.Invoke(ParseTransactionMessage(message.Payload), message.Sender);
            }
        }

        private BlockReceivedEventArgs ParseMinedBlockMessage(byte[] payload)
        {
            // _logger.LogDebug($"Payload: {HashHelper.ByteArrayToFormattedHexaString(payload)}");

            //int16 is transformed to an array of 2 bytes
            //max size of sidechain name is 12 chars/24 bytes
            //which can be reprensented in a short
            var lengthBytes = new byte[] { payload[0], payload[1] };
            // _logger.LogDebug($"Lenght Bytes: {HashHelper.ByteArrayToFormattedHexaString(lengthBytes)}");

            var length = BitConverter.ToInt16(lengthBytes);
            // _logger.LogDebug($"Lenght: {length}");

            byte[] stringBytes = new byte[length];
            Array.Copy(payload, lengthBytes.Length, stringBytes, 0, length);
            // _logger.LogDebug($"Sidechain Name Bytes: {HashHelper.ByteArrayToFormattedHexaString(stringBytes)}");

            var numberBlockBytes = payload.Length - length - lengthBytes.Length;
            byte[] blockBytes = new byte[numberBlockBytes];
            Array.Copy(payload, lengthBytes.Length + length, blockBytes, 0, numberBlockBytes);
            // _logger.LogDebug($"Block Bytes: {HashHelper.ByteArrayToFormattedHexaString(blockBytes)}");
            
            return new BlockReceivedEventArgs { SidechainName = Encoding.UTF8.GetString(stringBytes), BlockBytes = blockBytes };

        }

        //TODO: REFACTOR THIS
         private TransactionReceivedEventArgs ParseTransactionMessage(byte[] payload)
        {
            var lengthBytes = new byte[] { payload[0], payload[1] };

            var length = BitConverter.ToInt16(lengthBytes);

            byte[] stringBytes = new byte[length];
            Array.Copy(payload, lengthBytes.Length, stringBytes, 0, length);

            var numberBlockBytes = payload.Length - length - lengthBytes.Length;
            byte[] transactionBytes = new byte[numberBlockBytes];
            Array.Copy(payload, lengthBytes.Length + length, transactionBytes, 0, numberBlockBytes);
            
            return new TransactionReceivedEventArgs { SidechainName = Encoding.UTF8.GetString(stringBytes), TransactionBytes = transactionBytes };

        }


        private BlocksRequestReceivedEventArgs ParseRequestBlocksMessage(byte[] payload, IPEndPoint sender)
        {
            var beginBlockSequenceNumberBytes = new byte[8];
            Array.Copy(payload, 0, beginBlockSequenceNumberBytes, 0, 8);
            var endBlockSequenceNumberBytes = new byte[8];
            Array.Copy(payload, 8, endBlockSequenceNumberBytes, 0, 8);

            //  {payload[0], payload[1], payload[2], payload[3], payload[4], payload[5], payload[6], payload[7]}; 
            var beginBlockSequenceNumber = BitConverter.ToUInt64(beginBlockSequenceNumberBytes);
            var endBlockSequenceNumber = BitConverter.ToUInt64(endBlockSequenceNumberBytes);

            int sidechainBytesLength = payload.Length - 16;
            byte[] stringBytes = new byte[sidechainBytesLength];
            Array.Copy(payload, 16, stringBytes, 0, sidechainBytesLength);

            return new BlocksRequestReceivedEventArgs{ SidechainName = Encoding.UTF8.GetString(stringBytes), BeginBlockSequenceNumber = beginBlockSequenceNumber, EndBlockSequenceNumber = endBlockSequenceNumber, Sender = sender};
        }

        public event RecoverBlockReceivedEventHandler RecoverBlockReceived;
        public delegate void RecoverBlockReceivedEventHandler(BlockReceivedEventArgs args, IPEndPoint sender);

        public event MinedBlockReceivedEventHandler MinedBlockReceived;
        public delegate void MinedBlockReceivedEventHandler(BlockReceivedEventArgs args);

        public class BlockReceivedEventArgs
        {
            public string SidechainName { get; set; }
            public byte[] BlockBytes { get; set; }
        }

        public event BlocksRequestReceivedEventHandler BlocksRequestReceived;
        public delegate void BlocksRequestReceivedEventHandler(BlocksRequestReceivedEventArgs args);

        public class BlocksRequestReceivedEventArgs
        {
            public string SidechainName { get; set; }
            public ulong BeginBlockSequenceNumber { get; set; }
            public ulong EndBlockSequenceNumber { get; set; }
            public IPEndPoint Sender { get; set; }
        }

        public event IdentificationMessageReceivedEventHandler IdentificationMessageReceived;
        public delegate void IdentificationMessageReceivedEventHandler(IdentificationMessageReceivedEventArgs args);
        public class IdentificationMessageReceivedEventArgs
        {
            public IPEndPoint SenderIPEndPoint { get; set; }
            public string PublicKey { get; set; }
            public string EosAccount { get; set; }
        }

        public event TransactionReceivedEventHandler TransactionReceived;
        public delegate void TransactionReceivedEventHandler(TransactionReceivedEventArgs args, IPEndPoint sender);

        public class TransactionReceivedEventArgs
        {
            public byte[] TransactionBytes { get; set; }
            public string SidechainName { get; set; }
        }
    }

    public class MessageEvent
    {
        public NetworkMessageTypeEnum MessageType { get; set; }

        //returns true if message is the one needed
        public Func<NetworkMessage, bool> MessageValidator { get; set; }

        public Func<NetworkMessage, bool> ExceptionCases { get; set; }

        internal TaskCompletionSource<OpResult<NetworkMessage>> Awaitable { get; private set; }

        public DateTime? Timeout { get; private set; }

        public MessageEvent()
        {
            Awaitable = new TaskCompletionSource<OpResult<NetworkMessage>>();
        }

        public MessageEvent(DateTime? timeout)
        {
            Awaitable = new TaskCompletionSource<OpResult<NetworkMessage>>();
            Timeout = timeout;
        }
    }
}