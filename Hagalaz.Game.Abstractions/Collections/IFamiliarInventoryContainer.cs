using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    public interface IFamiliarInventoryContainer : IItemContainer
    {
        /// <summary>
        /// Deposit's specific item into familiars inventory.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// If depositing was successful.
        /// </returns>
        bool DepositFromInventory(IItem item, int count);
        /// <summary>
        /// Withdraws from familiar inventory.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        bool WithdrawFromFamiliarInventory(IItem item, int count);
    }
}
