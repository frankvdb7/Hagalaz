using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// Defines a contract for a loot generator, which processes loot tables to produce a list of items.
    /// </summary>
    public interface ILootGenerator
    {
        /// <summary>
        /// Generates loot based on a given set of parameters, such as the killer and the victim.
        /// </summary>
        /// <typeparam name="T">The type of loot item to be generated, which must implement <see cref="ILootItem"/>.</typeparam>
        /// <param name="lootParams">The parameters for the loot generation, including context about the event that triggered the loot drop.</param>
        /// <returns>A read-only list of <see cref="LootResult{T}"/>, each representing an item or a collection of items dropped.</returns>
        IReadOnlyList<LootResult<T>> GenerateLoot<T>(LootParams lootParams) where T : ILootItem;
    }
}