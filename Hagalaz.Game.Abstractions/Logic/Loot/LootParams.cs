using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    public record LootParams(IRandomTable<ILootObject> Table)
    {
        public int MaxCount { get; init; } = Table.MaxResultCount;

        public virtual LootContext ToContext(ILootObject loot) => new(loot);
    }

    public record CharacterLootParams(IRandomTable<ILootObject> Table, ICharacter Character) : LootParams(Table)
    {
        public override LootContext ToContext(ILootObject loot) =>
            new CharacterLootContext(loot)
            {
                Character = Character
            };
    }
}