using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="ILocation"/> interface and related types.
    /// </summary>
    public static class LocationExtensions
    {
        /// <summary>
        /// Converts any object that implements <see cref="IVector3"/> to a <see cref="Location"/> struct.
        /// </summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>A new <see cref="Location"/> instance with the same X, Y, and Z coordinates as the vector.</returns>
        public static Location ToLocation(this IVector3 vector) => Location.Create(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// Creates a new location by translating the current location by one step in the specified direction.
        /// </summary>
        /// <param name="location">The original location.</param>
        /// <param name="direction">The direction to translate in.</param>
        /// <returns>A new <see cref="ILocation"/> instance representing the translated position.</returns>
        public static ILocation Translate(this ILocation location, DirectionFlag direction) => location.Translate(direction.GetDeltaX(), direction.GetDeltaY(), 0);

        /// <summary>
        /// Calculates a unique hash for the region part where the location resides, based on its coordinates and height.
        /// </summary>
        /// <param name="location">The location for which to calculate the hash.</param>
        /// <returns>An integer representing the unique hash of the region part.</returns>
        public static int GetRegionPartHash(this ILocation location) => LocationHelper.GetRegionPartHash(location.RegionPartX, location.RegionPartY, location.Z);

        /// <summary>
        /// Calculates a unique hash for the location's position within its local region.
        /// </summary>
        /// <param name="location">The location for which to calculate the hash.</param>
        /// <returns>An integer representing the unique hash of the location within its local region.</returns>
        public static int GetRegionLocalHash(this ILocation location) => LocationHelper.GetRegionLocalHash(location.X, location.Y, location.Z);
    }
}