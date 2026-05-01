using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures
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
        {
            if (loc1.Z != loc2.Z || loc1.Dimension != loc2.Dimension)
            {
                return false;
            }

            int dx = 0;
            if (loc2.X > loc1.X + size1 - 1)
            {
                dx = loc2.X - (loc1.X + size1 - 1);
            }
            else if (loc1.X > loc2.X + size2 - 1)
            {
                dx = loc1.X - (loc2.X + size2 - 1);
            }

            int dy = 0;
            if (loc2.Y > loc1.Y + size1 - 1)
            {
                dy = loc2.Y - (loc1.Y + size1 - 1);
            }
            else if (loc1.Y > loc2.Y + size2 - 1)
            {
                dy = loc1.Y - (loc2.Y + size2 - 1);
            }

            // (int)Math.Sqrt(dx*dx + dy*dy) <= range is equivalent to dx*dx + dy*dy < (range + 1)^2
            // Using long to prevent overflow during squaring.
            long rangePlusOne = (long)range + 1;
            return (long)dx * dx + (long)dy * dy < rangePlusOne * rangePlusOne;
        }
    }
}
