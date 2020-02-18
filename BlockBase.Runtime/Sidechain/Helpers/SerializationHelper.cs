using System;
using BlockBase.Domain.Protos;
using Microsoft.Extensions.Logging;

namespace BlockBase.Runtime.Sidechain.Helpers
{
    public static class SerializationHelper
    {
        internal static BlockProto DeserializeBlock(byte[] payload, ILogger logger)
        {
            try
            {
                var blockProto = BlockProto.Parser.ParseFrom(payload);
                return blockProto;
            }
            catch (Exception e)
            {
                logger.LogCritical($"Failed to deserialize block. \nException thrown:{e.Message}");
                return null;
            }
        }

        internal static TransactionProto DeserializeTransaction(byte[] payload, ILogger logger)
        {
            try
            {
                var transactionProto = TransactionProto.Parser.ParseFrom(payload);
                return transactionProto;
            }
            catch (Exception e)
            {
                logger.LogCritical($"Failed to deserialize transaction. \nException thrown:{e.Message}");
                return null;
            }
        } 
    }
}