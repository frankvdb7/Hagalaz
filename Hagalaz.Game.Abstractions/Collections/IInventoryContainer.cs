using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a character's inventory container, which holds the items the character is carrying.
    /// </summary>
    public interface IInventoryContainer : IItemTransactional
    {
        IReadOnlyItemContainer Items { get; }

        bool AddItem(IItem item);
        bool AddItem(int slot, IItem item);
        bool AddItems(IEnumerable<IItem?> items);
        int RemoveItem(IItem item, int preferredSlot = -1, bool publishChanges = true);
        bool TryRemoveItemExact(IItem item, int preferredSlot = -1);
        bool TryTransferItemTo(IItemContainer destination, IItem item, int count,
            int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);
        bool TryTransferItemFrom(IItemContainer source, IItem item, int count,
            int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null);
        void ReplaceItem(int slot, IItem item);
        void SwapItems(int fromSlot, int toSlot);
        void ClearItems(bool publishChanges = true);

        /// <summary>Publishes committed inventory changes while the owning transaction scope is still active.</summary>
        void PublishChanges();

        /// <summary>
        /// Drops a specific item from the inventory onto the ground.
        /// </summary>
        /// <param name="item">The item to be dropped.</param>
        /// <returns><c>true</c> if the item was dropped successfully; otherwise, <c>false</c>.</returns>
        bool DropItem(IItem item);
    }
}
