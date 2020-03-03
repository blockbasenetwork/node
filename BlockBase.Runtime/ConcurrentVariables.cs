using System;
using System.Collections.Concurrent;
using System.Threading;
using BlockBase.Utils;

namespace BlockBase.Runtime
{
    public class ConcurrentVariables
    {
        public ConcurrentDictionary<string, SemaphoreSlim> DatabasesSemaphores;
        private const string _transactionNumberFileName = "transactionNumber.txt";
        private int _transactionNumber;
        private readonly object locker = new object();

        public ConcurrentVariables()
        {
            DatabasesSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _transactionNumber = LoadTransactionNumberFromFile();
        }

        public int GetNextTransactionNumber()
        {
            lock (locker)
            {
                _transactionNumber++;
                FileWriterReader.Write(_transactionNumberFileName
                 , _transactionNumber + "", System.IO.FileMode.OpenOrCreate);
                return _transactionNumber;
            }
        }

        private int LoadTransactionNumberFromFile()
        {
            lock (locker)
            {
                var lines = FileWriterReader.Read(_transactionNumberFileName);
                if (lines.Count == 0) return 0;
                return Int32.Parse(lines[0]);
            }
        }
    }
}