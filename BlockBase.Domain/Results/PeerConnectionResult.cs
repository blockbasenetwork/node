using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockBase.Domain.Results
{

    public class PeerConnectionResult
    {
        public string Name { get; set; }
        public string State { get; set; }
        public string Endpoint { get; set; }
    }
}
