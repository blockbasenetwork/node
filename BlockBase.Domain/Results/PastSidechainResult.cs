using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.Domain.Enums;

namespace BlockBase.Domain.Results
{
    public class PastSidechain
    {
        public string Name { get; set; }
        public DateTime SidechainCreationDate { get; set; }
        public DateTime DateLeft { get; set; }
        public string ReasonLeft { get; set; }
    }
}
