using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a reward container, which holds items that a player has earned but not yet claimed (e.g., from a quest or minigame).
    /// </summary>
    public interface IRewardContainer : IItemContainer
    {
        /// <summary>
        /// Claims a specific item from the reward container, moving it to the player's inventory or bank.
        /// </summary>
        /// <param name="item">The item to be claimed.</param>
        /// <param name="count">The number of items to claim.</param>
        /// <returns>The number of items that were actually claimed and transferred.</returns>
        int Claim(IItem item, int count);
    }
}
