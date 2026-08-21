using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Factories;

/// <summary>
/// Creates and restores familiars at the NPC composition boundary.
/// </summary>
public interface IFamiliarFactory
{
    /// <summary>
    /// Creates and spawns a new familiar for a character.
    /// </summary>
    /// <param name="summoner">The character summoning the familiar.</param>
    /// <param name="definition">The familiar definition.</param>
    /// <returns>A handle for the spawned familiar.</returns>
    INpcHandle Spawn(ICharacter summoner, SummoningDto definition);

    /// <summary>
    /// Restores the pending familiar for a character, if one exists.
    /// </summary>
    /// <param name="summoner">The character whose familiar is being restored.</param>
    /// <returns><c>true</c> when a familiar was restored; otherwise, <c>false</c>.</returns>
    bool TryRestore(ICharacter summoner);
}
