using System;
using System.Collections.Generic;

namespace Open.P2P.Utils
{
    internal class BlockingPool<T>
    {
        private readonly Func<T> _factory;
        private readonly Queue<T> _pool;

        public BlockingPool(Func<T> factory)
        {
            _factory = factory;
            _pool = new Queue<T>();
        }

        public int Count
        {
            get { return _pool.Count; }
        }

        public void Add(T item)
        {
            Guard.NotNull(item, "item");

            lock (_pool)
            {
                _pool.Enqueue(item);
            }
        }

        public T Take()
        {
            lock (_pool)
            {
                //rpinto - changed Dequeue to First, marciak - changed to Dequeue again
                //also added System.Linq
                return _pool.Count > 0 ? _pool.Dequeue() : _factory();
            }
        }
    }
}