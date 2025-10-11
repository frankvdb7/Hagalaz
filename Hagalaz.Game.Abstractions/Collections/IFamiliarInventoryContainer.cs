using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a familiar's inventory container, which holds items for a summoned creature (e.g., a beast of burden).
    /// </summary>
    public interface IFamiliarInventoryContainer : IItemContainer
    {
        /// <summary>
        /// Deposits a specific item from the player's inventory into the familiar's inventory.
        /// </summary>
        /// <param name="item">The item from the player's inventory to be deposited.</param>
        /// <param name="count">The number of items to deposit.</param>
        /// <returns><c>true</c> if the deposit was successful; otherwise, <c>false</c>.</returns>
        bool DepositFromInventory(IItem item, int count);

        /// <summary>
        /// Withdraws a specific item from the familiar's inventory, preparing it for placement in the player's inventory.
        /// </summary>
        /// <param name="item">The item in the familiar's inventory to be withdrawn.</param>
        /// <param name="count">The number of items to withdraw.</param>
        /// <returns><c>true</c> if the withdrawal was successful; otherwise, <c>false</c>.</returns>
        bool WithdrawFromFamiliarInventory(IItem item, int count);
    }
}
