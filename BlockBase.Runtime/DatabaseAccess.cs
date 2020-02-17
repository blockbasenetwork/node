using System.Collections.Concurrent;
using System.Threading;

namespace BlockBase.Runtime
{
    public class DatabaseAccess
    {
        public ConcurrentDictionary<string, SemaphoreSlim> DatabasesSemaphores;

        public DatabaseAccess()
        {
              DatabasesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }
    }
}