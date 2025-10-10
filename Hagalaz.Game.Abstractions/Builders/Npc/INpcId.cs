using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an NPC where the NPC's ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="INpcBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcId
    {
        /// <summary>
        /// Sets the unique identifier for the NPC type being built.
        /// </summary>
        /// <param name="id">The unique identifier for the NPC type.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the NPC's location.</returns>
        INpcLocation WithId(int id);
    }
}