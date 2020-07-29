using System;
using System.Net;

namespace BlockBase.Node.Commands.Utils
{
    public class CommandExecutionException
    {
        public HttpStatusCode StatusCode { get; set; }
        public Exception Exception { get; set; }
    }
}