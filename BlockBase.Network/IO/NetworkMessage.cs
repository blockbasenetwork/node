using BlockBase.Network.IO.Enums;
using BlockBase.Domain.Protos;
using BlockBase.Utils.Crypto;
using Google.Protobuf;
using System;
using System.Linq;
using System.Net;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;


namespace BlockBase.Network.IO
{
    public class NetworkMessage
    {
        /*******************************************************************************/
        /****These properties are only here to help internal forwarding and analysis****/

        //4 Bytes

        public byte[] MessageHash { get; set; }

        public string Signature { get; set; }

        public string PublicKey { get; set; }

        public IPEndPoint Destination { get; set; }

        //2 Bytes
        public NetworkMessageTypeEnum NetworkMessageType { get; set; }

        public object ParsedPayload { get; set; }
        public byte[] Payload { get; set; }

        public IPEndPoint Sender { get; set; }
        public TransportTypeEnum TransportType { get; set; }
        /*******************************************************************************/

        //4 Bytes
        public Int32 Version { get; set; }

        public NetworkMessage()
        {
            TransportType = TransportTypeEnum.Tcp;
        }

        public NetworkMessage(NetworkMessageTypeEnum networkMessageType, byte[] payload, TransportTypeEnum? transportType, string privateKey, string publicKey, string senderEndPoint, IPEndPoint destination = null)
        {
            NetworkMessageType = networkMessageType;
            Payload = payload;
            TransportType = transportType != null ? transportType.Value : TransportTypeEnum.Tcp;
            Destination = destination;
            PublicKey = publicKey;
            MessageHash = new byte[0];
            Signature = "";
            var ipAddressAndPort = senderEndPoint.Split(":");
            Sender = new IPEndPoint( IPAddress.Parse(ipAddressAndPort[0]), Int32.Parse(ipAddressAndPort[1]));

            FinalizeMessage(privateKey);
        }

        public bool CompareTo(NetworkMessage message) // rever metodo
        {
            for (int i = 0; i < message.Payload.Length; i++)
                if (this.Payload[i] != message.Payload[i]) return false;

            return true;
        }

        public byte[] ConvertToPacket() //rever metodo
        {
            var messageProto = new NetworkMessageProto()
            {
                NetworkMessageType = NetworkMessageType,
                Payload = Google.Protobuf.ByteString.CopyFrom(Payload),
                Version = Version,
                Signature = Signature,
                PublicKey = PublicKey,
                Destination = Destination.ToString(),
                MessageHash =  Google.Protobuf.ByteString.CopyFrom(MessageHash)
            };

            return messageProto.ToByteArray();
        }

        public Dictionary<string, object> ConvertToDictionary() //rever metodo
        {
            var dic = new Dictionary<string, object> ()
            {
                {"NetworkMessageType", NetworkMessageType.ToString()},
                {"MessageHash", MessageHash },
                {"Signature", Signature},
                {"PublicKey", PublicKey},
                {"Destination", Destination.ToString()},
                {"Payload", Payload},
                {"Sender", Sender.ToString()},
                {"TransportType", TransportType.ToString()}
            };
            return dic;
        }

        public static NetworkMessage BuildFromPacket(IPEndPoint sender, NetworkMessageProto networkMessageProto)
        {
            var networkMessage = new NetworkMessage();
            networkMessage.Sender = sender;
            networkMessage.NetworkMessageType = networkMessageProto.NetworkMessageType;
            networkMessage.Payload = networkMessageProto.Payload.ToByteArray();
            networkMessage.MessageHash = networkMessageProto.MessageHash.ToByteArray();
            networkMessage.PublicKey = networkMessageProto.PublicKey;
            networkMessage.Signature = networkMessageProto.Signature;
            networkMessage.Version = networkMessageProto.Version;
            var ipAddressAndPort = networkMessageProto.Destination.Split(":");
            networkMessage.Destination = new IPEndPoint(IPAddress.Parse(ipAddressAndPort[0]), Int32.Parse(ipAddressAndPort[1]));
            return networkMessage;
        }

        public void FinalizeMessage(string privateKey)
        {
            //Version = ConfigurationHelper.GetProducerVersion();
            var serializedMessage = JsonConvert.SerializeObject(this.ConvertToDictionary());
            MessageHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedMessage));
            Signature = SignatureHelper.SignHash(privateKey, MessageHash);
        }
    }
}