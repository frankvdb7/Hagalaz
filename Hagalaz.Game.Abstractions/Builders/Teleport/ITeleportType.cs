using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a teleport effect where the
    /// destination must be specified, either as a complete location object or by its individual coordinates.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ITeleportBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportType
    {
        /// <summary>
        /// Sets the teleport destination using an existing <see cref="ILocation"/> instance.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> representing the destination.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        ITeleportOptional WithLocation(ILocation location);

        /// <summary>
        /// Begins setting the teleport destination by specifying the X-coordinate.
        /// </summary>
        /// <param name="x">The X-coordinate of the destination.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the Y-coordinate.</returns>
        ITeleportY WithX(int x);
    }
}