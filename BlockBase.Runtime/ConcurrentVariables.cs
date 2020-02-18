using System.Collections.Concurrent;
using System.Threading;

namespace BlockBase.Runtime
{
    public class ConcurrentVariables
    {
        public ConcurrentDictionary<string, SemaphoreSlim> DatabasesSemaphores;
        private int _transactionNumber;

        public ConcurrentVariables()
        {
            DatabasesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _transactionNumber = 0;
        }

        public int GetNextTransactionNumber()
        {
            return Interlocked.Increment(ref _transactionNumber);
        }
    }
}