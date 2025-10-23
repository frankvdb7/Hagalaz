using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for vector-related interfaces like <see cref="IVector3"/>.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Calculates the X-coordinate of the region part (8x8 tile chunk) that this vector belongs to.
        /// </summary>
        /// <param name="vector">The vector to calculate from.</param>
        /// <returns>The X-coordinate of the region part.</returns>
        public static int GetRegionPartX(this IVector3 vector) => vector.X >> 3;

        /// <summary>
        /// Calculates the Y-coordinate of the region part (8x8 tile chunk) that this vector belongs to.
        /// </summary>
        /// <param name="vector">The vector to calculate from.</param>
        /// <returns>The Y-coordinate of the region part.</returns>
        public static int GetRegionPartY(this IVector3 vector) => vector.Y >> 3;

        /// <summary>
        /// Calculates the local X-coordinate within the client's viewport, relative to a central local vector.
        /// </summary>
        /// <param name="vector">The world vector to convert.</param>
        /// <param name="local">The central vector of the local viewport.</param>
        /// <param name="mapSize">The size of the client map (e.g., 104).</param>
        /// <returns>The local X-coordinate for the client.</returns>
        public static int GetLocalX(this IVector3 vector, IVector3 local, int mapSize) => vector.X - 8 * (local.GetRegionPartX() - (mapSize >> 4));

        /// <summary>
        /// Calculates the local Y-coordinate within the client's viewport, relative to a central local vector.
        /// </summary>
        /// <param name="vector">The world vector to convert.</param>
        /// <param name="local">The central vector of the local viewport.</param>
        /// <param name="mapSize">The size of the client map (e.g., 104).</param>
        /// <returns>The local Y-coordinate for the client.</returns>
        public static int GetLocalY(this IVector3 vector, IVector3 local, int mapSize) => vector.Y - 8 * (local.GetRegionPartY() - (mapSize >> 4));
    }
}