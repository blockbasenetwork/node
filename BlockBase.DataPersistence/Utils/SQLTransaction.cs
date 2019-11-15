using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace BlockBase.DataPersistence.Utils
{
    public class Transaction
    {
        public void ExecuteOperation(Action func)
        {

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(10)
            };
            using (var transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                func.Invoke();

                transactionScope.Complete();
            }
        }



        public T ExecuteOperation<T>(Func<T> func)
        {

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(10)
            };
            using (var transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                var result = func.Invoke();

                transactionScope.Complete();

                return result;
            }


        }
    }
}
    


