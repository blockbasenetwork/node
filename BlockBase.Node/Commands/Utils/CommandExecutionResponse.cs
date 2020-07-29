using System.Net;

namespace BlockBase.Node.Commands.Utils
{
    public class CommandExecutionResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public OperationResponse OperationResponse { get; set; }

        public CommandExecutionResponse() {}

        public CommandExecutionResponse(HttpStatusCode httpStatusCode, OperationResponse operationResponse)
        {
            HttpStatusCode = httpStatusCode;
            OperationResponse = operationResponse;
        }
    }
}