using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Location
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a location where the X-coordinate must be specified,
    /// or the location can be created from an existing source.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ILocationBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILocationX
    {
        /// <summary>
        /// Sets the X-coordinate for the location.
        /// </summary>
        /// <param name="x">The X-coordinate of the location.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the location's Y-coordinate.</returns>
        ILocationY WithX(int x);

        /// <summary>
        /// Sets the coordinates for the new location based on an existing <see cref="ILocation"/> instance.
        /// </summary>
        /// <param name="location">The source location from which to copy the coordinates.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        ILocationOptional FromLocation(ILocation location);

        /// <summary>
        /// Sets the coordinates for the new location based on a region ID. This typically sets the location to the base of the specified region.
        /// </summary>
        /// <param name="regionId">The ID of the region from which to derive the location.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        ILocationOptional FromRegionId(int regionId);
    }
}