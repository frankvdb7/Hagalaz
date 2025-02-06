using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Extensions
{
    public static class LocationExtensions
    {
        /// <summary>
        /// Converts a vector that implements the IVector3 to a Location struct
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Location ToLocation(this IVector3 vector) => Location.Create(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// Translates the specified direction.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public static ILocation Translate(this ILocation location, DirectionFlag direction) => location.Translate(direction.GetDeltaX(), direction.GetDeltaY(), 0);
        /// <summary>
        /// Gets the region part hash (without rotation).
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static int GetRegionPartHash(this ILocation location) => LocationHelper.GetRegionPartHash(location.RegionPartX, location.RegionPartY, location.Z);
        /// <summary>
        /// Gets the region local hash.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static int GetRegionLocalHash(this ILocation location) => LocationHelper.GetRegionLocalHash(location.X, location.Y, location.Z);
    }
}