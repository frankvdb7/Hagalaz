using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// Represents the context for a loot generation operation, containing the loot object and its quantity details.
    /// </summary>
    /// <param name="Loot">The loot object being processed.</param>
    public record LootContext(ILootObject Loot) : RandomObjectContext(Loot)
    {
        /// <summary>
        /// Gets the base minimum quantity of the loot item as defined in its loot table.
        /// </summary>
        public int BaseMinimumCount { get; init; }

        /// <summary>
        /// Gets the base maximum quantity of the loot item as defined in its loot table.
        /// </summary>
        public int BaseMaximumCount { get; init; }

        /// <summary>
        /// Gets or sets the modified minimum quantity of the loot item, which can be altered by modifiers (e.g., a ring of wealth).
        /// </summary>
        public int ModifiedMinimumCount { get; set; }

        /// <summary>
        /// Gets or sets the modified maximum quantity of the loot item, which can be altered by modifiers.
        /// </summary>
        public int ModifiedMaximumCount { get; set; }
    }

    /// <summary>
    /// Represents a specialized loot context that includes the character receiving the loot.
    /// This allows for loot modifications based on the character's stats, equipment, or other attributes.
    /// </summary>
    /// <param name="Loot">The loot object being processed.</param>
    public record CharacterLootContext(ILootObject Loot) : LootContext(Loot)
    {
        /// <summary>
        /// Gets the character involved in the loot generation event (e.g., the killer or the player opening a chest).
        /// </summary>
        public required ICharacter Character { get; init; }
    }
}