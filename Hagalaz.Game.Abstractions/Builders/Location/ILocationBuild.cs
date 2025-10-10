using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Location
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a location.
    /// This interface provides the method to construct the final <see cref="ILocation"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ILocationBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILocationBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="ILocation"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ILocation"/> object configured with the specified properties.</returns>
        ILocation Build();
    }
}