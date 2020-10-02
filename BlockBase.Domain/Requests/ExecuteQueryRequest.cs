using System.Collections.Generic;

namespace BlockBase.Domain.Requests
{
    public class ExecuteQueryRequest
    {
        public string Query { get; set; }
        public string Account { get; set; }
        public string Signature { get; set; }
    }
}