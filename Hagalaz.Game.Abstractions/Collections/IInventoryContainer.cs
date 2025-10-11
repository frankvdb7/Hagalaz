using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a character's inventory container, which holds the items the character is carrying.
    /// </summary>
    public interface IInventoryContainer : IItemContainer
    {
        /// <summary>
        /// Drops a specific item from the inventory onto the ground.
        /// </summary>
        /// <param name="item">The item to be dropped.</param>
        /// <returns><c>true</c> if the item was dropped successfully; otherwise, <c>false</c>.</returns>
        bool DropItem(IItem item);
    }
}
