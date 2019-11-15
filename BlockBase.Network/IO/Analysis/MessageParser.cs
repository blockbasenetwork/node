using BlockBase.Domain.Protos;
using BlockBase.Utils.Crypto;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace BlockBase.Network.IO.Analysis
{
    public class MessageParser
    {
        public MessageParsingResultEnum AnalyseAndParseMessage(RawNetworkMessage rawNetworkMessage, out NetworkMessage networkMessage)
        {
            //TODO: rpinto - voltar a pôr esta secção quando o RatingManager for novamente implementado
            //if (!RatingManager.Instance.IsSenderTrustful(sender)
            //    || RatingManager.Instance.IsSpammer(sender))
            //    return RecommendedActionEnum.Ignore;

            //if message is repeated
            //{
            //RatingManager.Instance.RecordIPEndPointBehavior(BehaviorTypeEnum.SentRepeatedMessage, sender);
            //return RecommendedActionEnum.Ignore;
            //}
            
            try
            {
                networkMessage = NetworkMessage.BuildFromPacket(rawNetworkMessage.IPEndPoint, NetworkMessageProto.Parser.ParseFrom(rawNetworkMessage.Bytes));
                
                if(networkMessage == null) Console.WriteLine("Network message is null.");
                Console.WriteLine("Received " + networkMessage.NetworkMessageType + " from " + networkMessage.Sender);
                ValidateNetworkMessage(networkMessage);
            }
            catch (Exception ex)
            {
                networkMessage = null;
                //RatingManager.Instance.RecordIPEndPointBehavior(BehaviorTypeEnum.SentMalformedMessage, sender);
                Console.WriteLine("Exception: " + ex.Message);
                return MessageParsingResultEnum.Failure;
            }

            //registar o comportamento do sender
            //recomendar uma acção

            //RatingManager.Instance.RecordIPEndPointBehavior(BehaviorTypeEnum.Normal, sender);
            return MessageParsingResultEnum.Success;
        }

        private void ValidateNetworkMessage(NetworkMessage networkMessage)
        {
            var receivedMessageHash = networkMessage.MessageHash;
            var receivedSignature = networkMessage.Signature;
     
            networkMessage.MessageHash = new byte[0];
            networkMessage.Signature = "";

            var serializedMessage = JsonConvert.SerializeObject(networkMessage.ConvertToDictionary());
            var messageHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedMessage));

            if(!messageHash.SequenceEqual(receivedMessageHash)) throw new FormatException("Wrong message hash.");
            if(!SignatureHelper.VerifySignature(networkMessage.PublicKey, receivedSignature, messageHash)) throw new FormatException("Wrong message signature.");
        }
    }
}