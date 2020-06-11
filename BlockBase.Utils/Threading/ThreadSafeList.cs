using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Utils.Threading
{
    public class ThreadSafeList<T>
    {
        private static readonly object Locker = new object();

        private List<T> _list = new List<T>();

        public void Add(T item)
        {
            lock (Locker)
            {
                _list.Add(item);
            }
        }

        public void Add(T item, Func<T, bool> avoidDuplicatesWhereClause)
        {
            lock (Locker)
            {
                if (_list.Where(avoidDuplicatesWhereClause).Any()) return;
                _list.Add(item);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (Locker)
            {
                _list.AddRange(items);
            }
        }

        public void Insert(int index, T item)
        {
            lock (Locker)
            {
                _list.Insert(index, item);
            }
        }

        public void Remove(T item)
        {
            lock (Locker)
            {
                _list.Remove(item);
            }
        }

        public void Clear()
        {
            lock (Locker)
            {
                _list.Clear();
            }
        }

        public void ClearAndAddRange(IEnumerable<T> items)
        {
            lock (Locker)
            {
                _list.Clear();
                _list.AddRange(items);
            }
        }

        public T Get(int pos)
        {
            lock (Locker)
            {
                if (pos >= 0 && _list.Count > pos)
                    return _list[pos];
                else throw new IndexOutOfRangeException();
            }
        }

        public bool Contains(Func<T, bool> whereClause)
        {
            lock(Locker)
            {
                return _list.Any(whereClause);
            }
        }

        public IEnumerable<T> GetEnumerable()
        {
            lock (Locker)
            {
                return _list.ToList();
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            lock (Locker)
            {
                return _list.ToList().GetEnumerator();
            }
        }

        public int Count()
        {
            lock (Locker)
            {
                return _list.ToList().Count();
            }
        }
    }
}