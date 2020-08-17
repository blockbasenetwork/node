using System;
using BlockBase.Domain.Blockchain;
using BlockBase.Network;
using BlockBase.Utils.Threading;

namespace BlockBase.Runtime.Provider
{
    public class ProducerConfirmationPoco
    {
        public DateTime LastConfirmationReceivedDateTime { get; set; }
        public string Producer { get; set; }
    }
}