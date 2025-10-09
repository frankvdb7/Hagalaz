using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Location
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a location where optional
    /// parameters like the Z-coordinate and dimension can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ILocationBuilder"/>.
    /// It also inherits from <see cref="ILocationBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILocationOptional : ILocationBuild
    {
        /// <summary>
        /// Sets the Z-coordinate (or plane) for the location.
        /// </summary>
        /// <param name="z">The Z-coordinate, representing the height level.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        ILocationOptional WithZ(int z);

        /// <summary>
        /// Sets the dimension for the location, allowing for instanced or alternate versions of the game world.
        /// </summary>
        /// <param name="dimension">The dimension identifier.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        ILocationOptional WithDimension(int dimension);

        /// <summary>
        /// Converts the current world coordinates to region-local coordinates.
        /// This is useful for calculations based on the region containing the location.
        /// </summary>
        /// <param name="localX">The local X-coordinate within the region.</param>
        /// <param name="localY">The local Y-coordinate within the region.</param>
        /// <param name="regionSizeX">The size of the region along the X-axis.</param>
        /// <param name="regionSizeY">The size of the region along the Y-axis.</param>
        /// <returns>The final build step of the fluent builder chain.</returns>
        ILocationBuild ToRegionCoordinates(int localX, int localY, int regionSizeX, int regionSizeY);
    }
}