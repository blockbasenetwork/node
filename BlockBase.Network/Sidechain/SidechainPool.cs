using BlockBase.Domain.Enums;
using BlockBase.Utils.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Sidechain
{
    public class SidechainPool
    {
        public string SmartContractAccount { get; set; }
        public ThreadSafeList<ProducerInPool> ProducersInPool { get; set; }
        public SidechainPoolStateEnum State { get; set; }
        public bool ProducingBlocks { get; set; }
        public bool CandidatureOnStandby { get; set; }

        public long NextStateWaitEndTime { get; set; }
        public uint BlocksBetweenSettlement { get; set; }
        public uint BlockTimeDuration { get; set; }
        public DateTime NextTimeToCheckSmartContract { get; set; }
 
        public SidechainPool()
        {
        }

        public SidechainPool(string smartContractAccount)
        {
            SmartContractAccount = smartContractAccount;
            State = SidechainPoolStateEnum.RecoverInfo;
            ProducersInPool = new ThreadSafeList<ProducerInPool>();
        }

        public override int GetHashCode()
        {
            return SmartContractAccount.GetHashCode();
        }


    }
}
