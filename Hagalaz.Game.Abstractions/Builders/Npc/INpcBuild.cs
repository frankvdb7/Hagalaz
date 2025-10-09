using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a Non-Player Character (NPC).
    /// This interface provides methods to either construct the <see cref="INpc"/> object
    /// or to construct and immediately spawn it in the game world.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="INpcBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcBuild
    {
        /// <summary>
        /// Builds the configured <see cref="INpc"/> instance without spawning it in the world.
        /// </summary>
        /// <returns>A new <see cref="INpc"/> object configured with the specified properties.</returns>
        INpc Build();

        /// <summary>
        /// Builds the configured <see cref="INpc"/> instance and immediately spawns it in the game world at its specified location.
        /// </summary>
        /// <returns>An <see cref="INpcHandle"/> to the newly spawned NPC, which can be used to manage its lifecycle.</returns>
        INpcHandle Spawn();
    }
}