using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a teleport effect where the Y-coordinate must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ITeleportBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportY
    {
        /// <summary>
        /// Sets the Y-coordinate for the teleport destination.
        /// </summary>
        /// <param name="y">The Y-coordinate of the destination.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        ITeleportOptional WithY(int y);
    }
}