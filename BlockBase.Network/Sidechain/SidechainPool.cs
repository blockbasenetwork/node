using BlockBase.Domain.Enums;
using BlockBase.Utils.Threading;
using System;


namespace BlockBase.Network.Sidechain
{
    public class SidechainPool
    {
        public string ClientAccountName { get; set; }
        public string ClientPublicKey { get; set; }

        public ThreadSafeList<ProducerInPool> ProducersInPool { get; set; }
        public SidechainPoolStateEnum State { get; set; }
        public bool ProducingBlocks { get; set; }
        public bool CandidatureOnStandby { get; set; }

        public long NextStateWaitEndTime { get; set; }
        public uint BlocksBetweenSettlement { get; set; }
        public uint BlockTimeDuration { get; set; }
        public uint BlockSizeInBytes { get; set; }
        public DateTime NextTimeToCheckSmartContract { get; set; }

        public TaskContainer ManagerTask { get; set; }
 
        public SidechainPool()
        {
        }

        public SidechainPool(string clientAccountName)
        {
            ClientAccountName = clientAccountName;
            State = SidechainPoolStateEnum.RecoverInfo;
            ProducersInPool = new ThreadSafeList<ProducerInPool>();
        }

        public override int GetHashCode()
        {
            return ClientAccountName.GetHashCode();
        }


    }
}
