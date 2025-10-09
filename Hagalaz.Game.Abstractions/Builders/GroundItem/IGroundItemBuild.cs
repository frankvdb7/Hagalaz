using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a ground item.
    /// This interface provides methods to either construct the <see cref="IGroundItem"/> object
    /// or to construct and immediately spawn it in the game world.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGroundItemBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemBuild
    {
        /// <summary>
        /// Builds the configured <see cref="IGroundItem"/> instance without spawning it in the world.
        /// </summary>
        /// <returns>A new <see cref="IGroundItem"/> object configured with the specified properties.</returns>
        IGroundItem Build();

        /// <summary>
        /// Builds the configured <see cref="IGroundItem"/> instance and immediately spawns it in the game world at its specified location.
        /// </summary>
        /// <returns>The newly spawned <see cref="IGroundItem"/> object.</returns>
        IGroundItem Spawn();
    }
}