using BlockBase.Domain.Blockchain;
using BlockBase.Network;
using BlockBase.Utils.Threading;

namespace BlockBase.Runtime.Provider
{
    public class TransactionSendingTrackPoco
    {
        public ThreadSafeList<string> ProducersAlreadyReceived { get; set; }
        public Transaction Transaction { get; set; }
    }
}