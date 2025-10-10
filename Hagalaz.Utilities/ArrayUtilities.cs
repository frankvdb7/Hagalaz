using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides utility methods for working with arrays.
    /// </summary>
    public static class ArrayUtilities
    {
        /// <summary>
        /// Combines multiple arrays of integers into a single enumerable collection.
        /// </summary>
        /// <param name="arrays">A variable number of integer arrays to be combined.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="int"/> containing all the elements from the input arrays in the order they were provided.</returns>
        public static IEnumerable<int> MakeArray(params int[][] arrays)
        {
            int total = arrays.Sum(t => t.Length);
            int[] array = new int[total];
            total = 0;
            foreach (var t in arrays)
                foreach (var t1 in t)
                    array[total++] = t1;

            return array;
        }
    }
}
