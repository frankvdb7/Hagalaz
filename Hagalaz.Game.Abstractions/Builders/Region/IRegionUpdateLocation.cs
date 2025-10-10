using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Region
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a region update where the location must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IRegionUpdateBuilder"/>.
    /// The location specified here typically serves as the base for the update, and further updates are relative to this location.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegionUpdateLocation
    {
        /// <summary>
        /// Sets the base location for the region update.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> that the region update will be based on.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters and the addition of specific updates.</returns>
        IRegionUpdateOptional WithLocation(ILocation location);
    }
}