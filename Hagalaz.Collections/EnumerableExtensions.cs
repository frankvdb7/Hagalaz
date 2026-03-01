using System.Collections.Generic;

namespace Hagalaz.Collections
{
    public static class EnumerableExtensions
    {
        public static ListHashSet<T> ToListHashSet<T>(this IEnumerable<T> source) where T : notnull
        {
            int capacity = 0;
            if (source is ICollection<T> collection)
            {
                capacity = collection.Count;
            }
            else if (source is IReadOnlyCollection<T> readOnlyCollection)
            {
                capacity = readOnlyCollection.Count;
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
