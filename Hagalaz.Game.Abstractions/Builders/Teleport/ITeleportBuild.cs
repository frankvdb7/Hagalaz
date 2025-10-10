using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a teleport effect.
    /// This interface provides the method to construct the final <see cref="ITeleport"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ITeleportBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="ITeleport"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ITeleport"/> object configured with the specified properties.</returns>
        ITeleport Build();
    }
}