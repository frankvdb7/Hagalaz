using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class ArrayUtilities
    {
        /// <summary>
        /// Make's one array from given arrays.
        /// </summary>
        /// <param name="arrays">The arrays.</param>
        /// <returns></returns>
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
