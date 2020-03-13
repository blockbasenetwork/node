using BlockBase.Domain.Blockchain;
using BlockBase.Network;
using BlockBase.Utils.Threading;

namespace BlockBase.Runtime.Sidechain
{
    public class TransactionSendingTrackPoco
    {
        public long NextTimeToSendTransaction { get; set; }
        public ThreadSafeList<PeerConnection> ProducersToSend { get; set; }
        public Transaction Transaction { get; set; }
    }
}