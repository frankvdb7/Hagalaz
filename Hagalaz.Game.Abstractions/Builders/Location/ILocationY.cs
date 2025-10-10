using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Location
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a location where the Y-coordinate must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ILocationBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILocationY
    {
        /// <summary>
        /// Sets the Y-coordinate for the location.
        /// </summary>
        /// <param name="y">The Y-coordinate of the location.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        ILocationOptional WithY(int y);
    }
}