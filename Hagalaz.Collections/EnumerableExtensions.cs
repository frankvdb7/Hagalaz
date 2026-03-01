using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Collections
{
    public static class EnumerableExtensions
    {
        public static ListHashSet<T> ToListHashSet<T>(this IEnumerable<T> source) where T : notnull
        {
            int capacity = 0;
            if (source.TryGetNonEnumeratedCount(out int count))
            {
                capacity = count;
            }

            var listHashSet = new ListHashSet<T>(capacity);
            foreach (var item in source)
            {
                listHashSet.Add(item);
            }
            return listHashSet;
        }
    }
}
