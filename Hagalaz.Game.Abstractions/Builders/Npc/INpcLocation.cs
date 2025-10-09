using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an NPC where the NPC's location must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="INpcBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcLocation
    {
        /// <summary>
        /// Sets the location for the NPC being built.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> where the NPC will be placed.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        INpcOptional WithLocation(ILocation location);
    }
}