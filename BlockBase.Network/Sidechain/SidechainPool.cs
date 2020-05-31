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
        public ProducerTypeEnum ProducerType { get; set; }
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

        public SidechainPool(string clientAccountName, ProducerTypeEnum producerType = 0)
        {
            ClientAccountName = clientAccountName;
            State = SidechainPoolStateEnum.Starting;
            ProducersInPool = new ThreadSafeList<ProducerInPool>();
            ProducerType = producerType;
        }

        public override int GetHashCode()
        {
            return ClientAccountName.GetHashCode();
        }


    }
}
