using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    public interface ILootGenerator
    {
        IReadOnlyList<LootResult<T>> GenerateLoot<T>(LootParams lootParams) where T : ILootItem;
    }
}