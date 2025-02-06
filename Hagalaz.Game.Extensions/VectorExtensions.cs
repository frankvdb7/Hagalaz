using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Extensions
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Gets the 8x8 splice X coordinate.
        /// </summary>
        /// <value>The region part X.</value>
        public static int GetRegionPartX(this IVector3 vector) => vector.X >> 3;

        /// <summary>
        /// Gets the 8x8 splice Y coordinate.
        /// </summary>
        /// <value>The region part Y.</value>
        public static int GetRegionPartY(this IVector3 vector) => vector.Y >> 3;

        public static int GetLocalX(this IVector3 vector, IVector3 local, int mapSize) => vector.X - 8 * (local.GetRegionPartX() - (mapSize >> 4));

        public static int GetLocalY(this IVector3 vector, IVector3 local, int mapSize) => vector.Y - 8 * (local.GetRegionPartY() - (mapSize >> 4));
    }
}