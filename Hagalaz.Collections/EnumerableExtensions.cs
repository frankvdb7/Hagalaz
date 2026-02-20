using System.Collections.Generic;

namespace Hagalaz.Collections
{
    public static class EnumerableExtensions
    {
        public static ListHashSet<T> ToListHashSet<T>(this IEnumerable<T> source) where T : notnull
        {
            var listHashSet = new ListHashSet<T>();
            foreach (var item in source)
            {
                listHashSet.Add(item);
            }
            return listHashSet;
        }
    }
}
