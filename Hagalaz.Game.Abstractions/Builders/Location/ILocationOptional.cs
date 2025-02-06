namespace Hagalaz.Game.Abstractions.Builders.Location
{
    public interface ILocationOptional : ILocationBuild
    {
        /// <summary>
        /// Sets the z coordinate of the location.
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        ILocationOptional WithZ(int z);
        /// <summary>
        /// Sets the dimension coordinate of the location.
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        ILocationOptional WithDimension(int dimension);
        /// <summary>
        /// Converts the coordinates of the location to a region location
        /// </summary>
        /// <param name="localX"></param>
        /// <param name="localY"></param>
        /// <param name="regionSizeX"></param>
        /// <param name="regionSizeY"></param>
        /// <returns></returns>
        ILocationBuild ToRegionCoordinates(int localX, int localY, int regionSizeX, int regionSizeY);
    }
}