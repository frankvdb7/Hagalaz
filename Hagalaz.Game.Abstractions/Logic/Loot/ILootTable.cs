using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// Defines a contract for a loot table, which is a collection of potential loot objects (items or other tables) that can be dropped.
    /// It inherits from <see cref="ILootObject"/>, allowing loot tables to be nested within other loot tables.
    /// </summary>
    public interface ILootTable : ILootObject, IRandomTable<ILootObject>
    {
    }
}