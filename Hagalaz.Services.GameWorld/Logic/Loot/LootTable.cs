using Hagalaz.Game.Abstractions.Logic.Loot;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    /// <summary>
    /// Represents a single LootTable.
    /// </summary>
    public class LootTable : RandomTableBase<ILootObject>, ILootTable
    {
    }
}