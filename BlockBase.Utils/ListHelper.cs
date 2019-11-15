using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BlockBase.Utils
{
    public static class ListHelper
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        
        public static List<T> GetListSortedCountingFrontFromIndex<T>(List<T> unsortedList, int index)
        {
            var auxiliar = unsortedList.GetRange(index, unsortedList.Count() - index);

            unsortedList = auxiliar.Concat(unsortedList.Except(auxiliar)).ToList();

            unsortedList.RemoveAt(0);

            return unsortedList;

        }

        public static List<T> GetListSortedCountingBackFromIndex<T>(List<T> unsortedList, int index)
        {
            unsortedList = GetListSortedCountingFrontFromIndex(unsortedList, index);

            unsortedList.Reverse();

            return unsortedList;

        }
    }
}
