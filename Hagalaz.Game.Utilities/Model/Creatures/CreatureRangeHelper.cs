using System;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Utilities.Model.Creatures
{
    /// <summary>
    /// Provides utility methods for calculating distances and proximity between creatures and locations.
    /// </summary>
    public static class CreatureRangeHelper
    {
        /// <summary>
        /// Determines whether two axis-aligned bounding boxes (representing creatures) are within a specified distance.
        /// </summary>
        /// <param name="loc1">The location of the first creature (bottom-left corner).</param>
        /// <param name="size1">The size of the first creature.</param>
        /// <param name="loc2">The location of the second creature (bottom-left corner).</param>
        /// <param name="size2">The size of the second creature.</param>
        /// <param name="range">The maximum allowed Euclidean distance.</param>
        /// <returns><c>true</c> if the distance between the closest points of the two boxes is within <paramref name="range"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is optimized to O(1) using Axis-Aligned Bounding Box (AABB) distance calculation.
        /// It avoids expensive square root operations by comparing squared distances.
        /// </remarks>
        public static bool IsWithinRange(ILocation loc1, int size1, ILocation loc2, int size2, int range)
            => IsWithinRange<ILocation, ILocation>(loc1, size1, loc2, size2, range);

        /// <summary>
        /// Determines whether two axis-aligned bounding boxes (representing creatures) are within a specified distance.
        /// This generic version allows for devirtualization of property accesses when using concrete types like <see cref="Location"/>.
        /// </summary>
        /// <typeparam name="T1">The type of the first location.</typeparam>
        /// <typeparam name="T2">The type of the second location.</typeparam>
        /// <param name="loc1">The location of the first creature.</param>
        /// <param name="size1">The size of the first creature.</param>
        /// <param name="loc2">The location of the second creature.</param>
        /// <param name="size2">The size of the second creature.</param>
        /// <param name="range">The maximum allowed Euclidean distance.</param>
        /// <returns><c>true</c> if within range; otherwise, <c>false</c>.</returns>
        public static bool IsWithinRange<T1, T2>(T1 loc1, int size1, T2 loc2, int size2, int range)
            where T1 : ILocation
            where T2 : ILocation
        {
            if (range < 0 || loc1.Z != loc2.Z || loc1.Dimension != loc2.Dimension)
            {
                return false;
            }

            int x1 = loc1.X;
            int y1 = loc1.Y;
            int x2 = loc2.X;
            int y2 = loc2.Y;

            int dx = 0;
            int x1Max = x1 + size1 - 1;
            if (x2 > x1Max)
            {
                dx = x2 - x1Max;
            }
            else
            {
                int x2Max = x2 + size2 - 1;
                if (x1 > x2Max)
                {
                    dx = x1 - x2Max;
                }
            }

            int dy = 0;
            int y1Max = y1 + size1 - 1; // Note: size1 is used for both X and Y as creatures are square
            if (y2 > y1Max)
            {
                dy = y2 - y1Max;
            }
            else
            {
                int y2Max = y2 + size2 - 1;
                if (y1 > y2Max)
                {
                    dy = y1 - y2Max;
                }
            }

            // (int)Math.Sqrt(dx*dx + dy*dy) <= range is equivalent to dx*dx + dy*dy < (range + 1)^2
            // Using long to prevent overflow during squaring.
            long rangePlusOne = (long)range + 1;
            return (long)dx * dx + (long)dy * dy < rangePlusOne * rangePlusOne;
        }
    }
}
