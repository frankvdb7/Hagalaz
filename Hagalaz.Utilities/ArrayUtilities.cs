using System;
using System.Collections.Generic;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides utility methods for working with arrays.
    /// </summary>
    public static class ArrayUtilities
    {
        /// <summary>
        /// Combines multiple arrays of integers into a single array.
        /// </summary>
        /// <param name="arrays">A variable number of integer arrays to be combined. Null inner arrays are ignored.</param>
        /// <returns>An array of <see cref="int"/> containing all the elements from the input arrays in the order they were provided.</returns>
        public static int[] MakeArray(params int[][] arrays)
        {
            if (arrays == null || arrays.Length == 0)
            {
                return [];
            }

            // Optimization: Use a manual loop to calculate total length to avoid LINQ Sum overhead.
            int total = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                int[] inner = arrays[i];
                if (inner != null)
                {
                    total += inner.Length;
                }
            }

            int[] result = new int[total];
            int offset = 0;

            // Optimization: Use Array.Copy for high-performance block copying instead of nested foreach loops.
            for (int i = 0; i < arrays.Length; i++)
            {
                int[] source = arrays[i];
                if (source != null)
                {
                    int length = source.Length;
                    if (length > 0)
                    {
                        Array.Copy(source, 0, result, offset, length);
                        offset += length;
                    }
                }
            }

            return result;
        }
    }
}
