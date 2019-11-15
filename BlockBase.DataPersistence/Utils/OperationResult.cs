using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataPersistence.Utils
{

    public class OperationResult
    {
        protected bool _hasSucceededBeenChecked;

        private bool _hasSucceeded;

        public bool HasSucceeded
        {
            get
            {
                _hasSucceededBeenChecked = true;
                return _hasSucceeded;
            }
            set { _hasSucceeded = value; }
        }

        public Exception Exception { get; set; }

        public OperationResult(Exception ex)
        {
            Exception = ex;
            HasSucceeded = false;
        }

        public OperationResult()
        {
        }

        public OperationResult(bool hasSucceeded)
        {
            HasSucceeded = hasSucceeded;
        }
    }

    public class OperationResult<T> : OperationResult
    {
        private T _result;

        public T Result
        {
            get
            {
                if (_hasSucceededBeenChecked) return _result;
                else throw new Exception("You must check if operation has succeeded!");
            }
            set { _result = value; }
        }

        public OperationResult()
        {
        }

        public OperationResult(T result)
        {
            Result = result;
            HasSucceeded = true;
        }

        public OperationResult(Exception ex)
        {
            Exception = ex;
            HasSucceeded = false;
        }
    }
}
