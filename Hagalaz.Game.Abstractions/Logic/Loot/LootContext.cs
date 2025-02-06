using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    public record LootContext(ILootObject Loot) : RandomObjectContext(Loot)
    {
        public int BaseMinimumCount { get; init; }
        public int BaseMaximumCount { get; init; }
        public int ModifiedMinimumCount { get; set; }
        public int ModifiedMaximumCount { get; set; }
    }

    public record CharacterLootContext(ILootObject Loot) : LootContext(Loot)
    {
        public required ICharacter Character { get; init; }
    }
}