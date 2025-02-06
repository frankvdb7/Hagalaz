using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Location
{
    public interface ILocationX
    {
        /// <summary>
        /// Sets the x coordinate on the location
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        ILocationY WithX(int x);
        /// <summary>
        /// Sets the coordinates of this location from the supplied location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        ILocationOptional FromLocation(ILocation location);
        /// <summary>
        /// Sets the coordinates of this location from the supplied region id.
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        ILocationOptional FromRegionId(int regionId);
    }
}