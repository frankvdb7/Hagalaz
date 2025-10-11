using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// Represents the parameters for a loot generation request, specifying the loot table to use.
    /// </summary>
    /// <param name="Table">The primary loot table from which to generate loot.</param>
    public record LootParams(IRandomTable<ILootObject> Table)
    {
        /// <summary>
        /// Gets the maximum number of loot results to generate from the table. Defaults to the table's own maximum result count.
        /// </summary>
        public int MaxCount { get; init; } = Table.MaxResultCount;

        /// <summary>
        /// Creates a loot context for a given loot object, to be used by loot modifiers.
        /// </summary>
        /// <param name="loot">The loot object to create a context for.</param>
        /// <returns>A new <see cref="LootContext"/> for the specified loot object.</returns>
        public virtual LootContext ToContext(ILootObject loot) => new(loot);
    }

    /// <summary>
    /// Represents specialized loot parameters that include the character involved in the loot event.
    /// </summary>
    /// <param name="Table">The primary loot table from which to generate loot.</param>
    /// <param name="Character">The character receiving the loot.</param>
    public record CharacterLootParams(IRandomTable<ILootObject> Table, ICharacter Character) : LootParams(Table)
    {
        /// <summary>
        /// Creates a character-specific loot context for a given loot object.
        /// </summary>
        /// <param name="loot">The loot object to create a context for.</param>
        /// <returns>A new <see cref="CharacterLootContext"/> that includes the character.</returns>
        public override LootContext ToContext(ILootObject loot) =>
            new CharacterLootContext(loot)
            {
                Character = Character
            };
    }
}