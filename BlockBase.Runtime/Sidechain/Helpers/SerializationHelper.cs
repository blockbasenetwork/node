using System;
using System.Collections.Generic;
using System.Linq;
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

        internal static IList<BlockProto> DeserializeBlocks(byte[] payload, ILogger logger)
        {
            try
            {
                var blockProtos = new List<BlockProto>();
                for (int i = 0; i < payload.Length;)
                {
                    var countBytes = new byte[8];
                    Array.Copy(payload, i, countBytes, 0, 4);
                    var count = BitConverter.ToInt32(countBytes);
                    i += 4;
                    var blockBytes = new byte[count];
                    Array.Copy(payload, i, blockBytes, 0, count);
                    i += count;

                    var blockProto = BlockProto.Parser.ParseFrom(payload);
                    blockProtos.Add(blockProto);
                }
                return blockProtos;
            }
            catch (Exception e)
            {
                logger.LogCritical($"Failed to deserialize block. \nException thrown:{e.Message}");
                return null;
            }
        }

        internal static IList<TransactionProto> DeserializeTransactions(byte[] payload, ILogger logger)
        {
            try
            {
                var transactionProtos = new List<TransactionProto>();
                for (int i = 0; i < payload.Length;)
                {
                    var countBytes = new byte[8];
                    Array.Copy(payload, i, countBytes, 0, 4);
                    var count = BitConverter.ToInt32(countBytes);
                    i += 4;
                    var transactionBytes = new byte[count];
                    Array.Copy(payload, i, transactionBytes, 0, count);
                    i += count;

                    var transactionProto = TransactionProto.Parser.ParseFrom(transactionBytes);
                    transactionProtos.Add(transactionProto);
                }
                return transactionProtos;
            }
            catch (Exception e)
            {
                logger.LogCritical($"Failed to deserialize transaction. \nException thrown:{e.Message}");
                return null;
            }
        }
    }
}