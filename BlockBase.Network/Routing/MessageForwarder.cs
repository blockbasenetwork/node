using BlockBase.Domain.Protos;
using BlockBase.Network.Exceptions;
using BlockBase.Network.IO;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Operation;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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

                else if (message.NetworkMessageType == NetworkMessageTypeEnum.Ping) PingReceived?.Invoke(ParsePingMessage(message.Payload), message.Sender);

                else if (message.NetworkMessageType == NetworkMessageTypeEnum.SendBlock) RecoverBlockReceived?.Invoke(ParseMinedBlockMessage(message.Payload), message.Sender);

                else if(message.NetworkMessageType == NetworkMessageTypeEnum.SendProducerIdentification)IdentificationMessageReceived?.Invoke(new IdentificationMessageReceivedEventArgs {PublicKey = message.PublicKey, EosAccount = message.EosAccount, SenderIPEndPoint = message.Sender});

                else if(message.NetworkMessageType == NetworkMessageTypeEnum.RequestBlocks) BlocksRequestReceived?.Invoke(ParseRequestBlocksMessage(message.Payload, message.Sender));

                else if(message.NetworkMessageType == NetworkMessageTypeEnum.SendTransaction) TransactionReceived?.Invoke(ParseTransactionMessage(message.Payload), message.Sender);
            }
        }

        private BlockReceivedEventArgs ParseMinedBlockMessage(byte[] payload)
        {
            var clientAccountNameAndBlockBytes = ParseClienAccounttName(payload);
            
            
            return new BlockReceivedEventArgs { ClientAccountName = clientAccountNameAndBlockBytes.Item1, BlockBytes = clientAccountNameAndBlockBytes.Item2 };

        }

         private TransactionReceivedEventArgs ParseTransactionMessage(byte[] payload)
        {
            var clientAccountNameAndTransactionBytes = ParseClienAccounttName(payload);
            
            return new TransactionReceivedEventArgs { ClientAccountName = clientAccountNameAndTransactionBytes.Item1, TransactionBytes = clientAccountNameAndTransactionBytes.Item2 };
        }

        private Tuple<string, byte[]> ParseClienAccounttName(byte[] payload)
        {
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
            byte[] transactionBytes = new byte[numberBlockBytes];
            Array.Copy(payload, lengthBytes.Length + length, transactionBytes, 0, numberBlockBytes);
            // _logger.LogDebug($"Block Bytes: {HashHelper.ByteArrayToFormattedHexaString(blockBytes)}");

            return new Tuple<string, byte[]> (Encoding.UTF8.GetString(stringBytes), transactionBytes);
        }


        private BlocksRequestReceivedEventArgs ParseRequestBlocksMessage(byte[] payload, IPEndPoint sender)
        {
            var numberBlocksBytes = new byte[4];
            Array.Copy(payload, 0, numberBlocksBytes, 0, 4);
            var numberOfBlocks = BitConverter.ToInt32(numberBlocksBytes);
            _logger.LogDebug("Payload size " + payload.Length + " bytes.");
            _logger.LogDebug("Number of blocks to send: " + numberOfBlocks);
            var sequenceNumbers = new List<ulong>();

            int i;

            for(i = 4; i < numberOfBlocks * 8 + 4; i += 8)
            {
                // _logger.LogDebug("Index: " + i);
                var sequenceNumberBytes = new byte[8];
                Array.Copy(payload, i, sequenceNumberBytes, 0, 8);
                var sequenceNumber = BitConverter.ToUInt64(sequenceNumberBytes);
                sequenceNumbers.Add(sequenceNumber);
            }

            
            int sidechainBytesLength = payload.Length - i;
            byte[] stringBytes = new byte[sidechainBytesLength];
            Array.Copy(payload, i, stringBytes, 0, sidechainBytesLength);

            return new BlocksRequestReceivedEventArgs{ ClientAccountName = Encoding.UTF8.GetString(stringBytes), BlocksSequenceNumber = sequenceNumbers, Sender = sender};
        }

        private PingReceivedEventArgs ParsePingMessage(byte[] payload)
        {
            var nonce = BitConverter.ToInt32(payload);

            return new PingReceivedEventArgs{ nonce = nonce };
        }

        public event RecoverBlockReceivedEventHandler RecoverBlockReceived;
        public delegate void RecoverBlockReceivedEventHandler(BlockReceivedEventArgs args, IPEndPoint sender);

        public event MinedBlockReceivedEventHandler MinedBlockReceived;
        public delegate void MinedBlockReceivedEventHandler(BlockReceivedEventArgs args);

        public class BlockReceivedEventArgs
        {
            public string ClientAccountName { get; set; }
            public byte[] BlockBytes { get; set; }
        }

        public event BlocksRequestReceivedEventHandler BlocksRequestReceived;
        public delegate void BlocksRequestReceivedEventHandler(BlocksRequestReceivedEventArgs args);

        public class BlocksRequestReceivedEventArgs
        {
            public string ClientAccountName { get; set; }
            public IList<ulong> BlocksSequenceNumber { get; set; }
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

        public event PingReceivedEventHandler PingReceived;
        public delegate void PingReceivedEventHandler(PingReceivedEventArgs args, IPEndPoint sender);
        public class PingReceivedEventArgs
        {
            public int nonce { get; set; }
        }

        public event TransactionReceivedEventHandler TransactionReceived;
        public delegate void TransactionReceivedEventHandler(TransactionReceivedEventArgs args, IPEndPoint sender);

        public class TransactionReceivedEventArgs
        {
            public byte[] TransactionBytes { get; set; }
            public string ClientAccountName { get; set; }
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