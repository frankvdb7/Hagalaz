using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Store
{
    /// <summary>
    /// Provides synchronously accessible summoning definitions that were loaded during world startup.
    /// </summary>
    public interface ISummoningDefinitionStore
    {
        /// <summary>
        /// Finds a summoning definition by familiar NPC ID.
        /// </summary>
        /// <param name="npcId">The familiar NPC ID.</param>
        /// <returns>The definition if one exists; otherwise, <c>null</c>.</returns>
        SummoningDto? FindByNpcId(int npcId);
    }
}
