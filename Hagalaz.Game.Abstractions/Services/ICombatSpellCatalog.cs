using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Serves the persisted combat spell definitions of every spellbook from memory, so widget and combat code never waits on the database.
    /// </summary>
    public interface ICombatSpellCatalog
    {
        /// <summary>
        /// Gets all combat spell definitions, loaded once at startup.
        /// </summary>
        IReadOnlyList<CombatSpellDto> CombatSpells { get; }
    }
}
