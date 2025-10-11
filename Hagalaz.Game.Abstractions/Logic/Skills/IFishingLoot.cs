using Hagalaz.Game.Abstractions.Logic.Loot;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Defines the contract for a loot item specifically obtained from fishing, including the experience gained and level required.
    /// </summary>
    public interface IFishingLoot : ILootItem
    {
        /// <summary>
        /// Gets the amount of fishing experience awarded for catching this item.
        /// </summary>
        double FishingExperience { get; init; }

        /// <summary>
        /// Gets the required fishing level to be able to catch this item.
        /// </summary>
        int RequiredLevel { get; init; }
    }
}