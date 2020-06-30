using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Node
{
    public class OperationResponse<T>
    {
        public bool Succeeded { get; set; }
        public Exception Exception { get; set; }
        public T Response { get; set; }
        public string ResponseMessage { get; set; }

        public OperationResponse() { }

        public OperationResponse(Exception ex, string message = null)
        {
            Succeeded = false;
            Exception = ex;
            ResponseMessage = message;
        }

        public OperationResponse(bool success, string message)
        {
            Succeeded = success;
            Response = default(T);
            ResponseMessage = message;
        }

        public OperationResponse(T response, string message = null)
        {
            Succeeded = true;
            Response = response;
            ResponseMessage = message;
        }
    }
}