using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    /// <summary>
    /// Represents a single LootTable.
    /// </summary>
    public class LootTable : RandomTableBase<ILootObject>, ILootTable
    {
        public LootTable(int id, string name, List<ILootObject> entries, List<IRandomObjectModifier> modifiers) : base(id, name, entries, modifiers) { }
    }
}