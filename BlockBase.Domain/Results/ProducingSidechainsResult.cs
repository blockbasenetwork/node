using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Enums;

namespace BlockBase.Domain.Results
{
    public class ProducingSidechain
    {
        public string Name { get; set; }
        public SidechainPoolStateEnum SidechainState { get; set; }
        public int BlocksProducedInCurrentSettlement { get; set; }
        public int BlocksFailedInCurrentSettlement { get; set; }
        public List<ProducingSidechainWarning> Warnings { get; set; }

        public ProducingSidechain()
        {
            Warnings = new List<ProducingSidechainWarning>();
        }
    }

    public class ProducingSidechainWarning
    {
        public WarningTypeEnum WarningType { get; set;}
        public DateTime WarningTimestamp { get; set; }
    }
}
