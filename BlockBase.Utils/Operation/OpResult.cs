using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Operation
{
    public class OpResult
    {
        public bool Succeeded { get; set; }
        public Exception Exception { get; set; }

        public OpResult() { }

        public OpResult(bool succeeded)
        {
            Succeeded = succeeded;
        }

        public OpResult(Exception ex)
        {
            Succeeded = false;
            Exception = ex;
        }

        public OpResult(bool succedded, Exception ex)
        {
            Succeeded = succedded;
            Exception = ex;
        }
    }

    public class OpResult<Tout> : OpResult
    {
        public Tout Result { get; set; }

        public OpResult(Tout result)
        {
            Succeeded = true;
            Result = result;
        }

        public OpResult(Exception ex)
        {
            Succeeded = false;
            Exception = ex;
        }
    }
}
