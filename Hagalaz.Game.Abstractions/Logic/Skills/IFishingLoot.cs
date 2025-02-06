using Hagalaz.Game.Abstractions.Logic.Loot;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    public interface IFishingLoot : ILootItem
    {
        double FishingExperience { get; init; }
        int RequiredLevel { get; init; }
    }
}