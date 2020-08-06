using BlockBase.Domain.Protos;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace BlockBase.Network.IO.Analysis
{
    public class MessageParser
    {
        ILogger _logger;
        public MessageParser(ILogger logger)
        {
            _logger = logger;
        }

        public MessageParsingResultEnum AnalyseAndParseMessage(RawNetworkMessage rawNetworkMessage, out NetworkMessage networkMessage)
        {
            //TODO: rpinto - add this section to the rating manager when it's implemented again
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
                ValidateNetworkMessage(networkMessage);
            }
            catch (Exception e)
            {
                networkMessage = null;
                //RatingManager.Instance.RecordIPEndPointBehavior(BehaviorTypeEnum.SentMalformedMessage, sender);
                
                _logger.LogWarning($"Unable to parse network message from {rawNetworkMessage.IPEndPoint.Address.ToString()}:{rawNetworkMessage.IPEndPoint.Port}");
                _logger.LogDebug($"Exception {e}")
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