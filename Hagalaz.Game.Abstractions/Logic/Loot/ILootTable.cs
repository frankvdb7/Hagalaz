using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    public interface ILootTable : ILootObject, IRandomTable<ILootObject>
    {
    }
}