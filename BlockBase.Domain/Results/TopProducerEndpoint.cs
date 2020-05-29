using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockBase.Domain.Results
{
    public class TopProducerEndpoint
    {
        public EosSharp.Core.Api.v1.Producer ProducerInfo { get; set; }
        public List<string> Endpoints { get; set; }
    }

    public class TopProducerEndpointResponse
    {
        public EosSharp.Core.Api.v1.Producer ProducerInfo { get; set; }
        public List<EndpointResponse> Endpoints { get; set; }
    }

    public class EndpointResponse
    {
        public string Endpoint { get; set; }
        public long ResponseTimeInMs { get; set; }
    }
}
