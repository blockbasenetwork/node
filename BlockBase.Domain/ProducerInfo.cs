using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BlockBase.Domain
{
    public class ProducerInfo
    {
        public string PublicKey { get; set; }

        public string AccountName { get; set; }

        public IPEndPoint IPEndPoint { get; set; }

        public bool NewlyJoined { get; set; }

//       public long StakeInMiniCoins { get; set; }

//        public int NumberOfBlocksToWork { get; set; }

//        public int StartedWorkingAtBlockWithSequenceNumber { get; set; }
    }
}
