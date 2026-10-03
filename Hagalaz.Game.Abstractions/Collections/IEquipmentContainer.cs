using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a character's equipment container, which manages the items a character is currently wearing.
    /// </summary>
    public interface IEquipmentContainer
    {
        /// <summary>
        /// Gets read-only indexed and enumerable access to equipped items.
        /// </summary>
        IReadOnlyItemContainer Items { get; }

        /// <summary>
        /// Gets the item in the specified equipment slot.
        /// </summary>
        /// <param name="index">The equipment slot to retrieve the item from.</param>
        /// <returns>The <see cref="IItem"/> in the specified slot, or <c>null</c> if the slot is empty.</returns>
        IItem? this[EquipmentSlot index] { get; }

        /// <summary>
        /// Equips an item from another container (e.g., inventory) to the appropriate slot on the character.
        /// </summary>
        /// <param name="item">The item to equip.</param>
        /// <returns><c>true</c> if the item was equipped successfully; otherwise, <c>false</c>.</returns>
        bool EquipItem(IItem item);

        /// <summary>
        /// Unequips an item from the character and prepares it for placement in another container.
        /// </summary>
        /// <param name="item">The item to unequip.</param>
        /// <param name="toInventorySlot">The preferred destination slot in the inventory. If -1, the item will be placed in the first available slot.</param>
        /// <returns><c>true</c> if the item was unequipped successfully; otherwise, <c>false</c>.</returns>
        bool UnEquipItem(IItem item, int toInventorySlot = -1);

        /// <summary>Moves equipped items to another ordinary item container, preserving equipment callbacks.</summary>
        bool TryMoveTo(IItemContainer destination, IItem item, int count, EquipmentSlot slot, IItem? destinationItem = null);

        /// <summary>
        /// Gets the equipment slot of a specific item instance currently worn by the character.
        /// </summary>
        /// <param name="instance">The exact item instance to find.</param>
        /// <returns>The <see cref="EquipmentSlot"/> where the item is equipped, or <see cref="EquipmentSlot.NoSlot"/> if not found.</returns>
        EquipmentSlot GetInstanceSlot(IItem instance);

        /// <summary>Restores an already-equipped item into its equipment slot without running equip lifecycle callbacks.</summary>
        bool TryRestoreEquippedItem(EquipmentSlot slot, IItem item);

        /// <summary>Replaces an equipped item only if the expected instance still occupies the slot.</summary>
        bool TryReplaceEquippedItem(EquipmentSlot slot, IItem expectedItem, IItem replacement);

        /// <summary>Removes an equipped item and publishes the resulting equipment change.</summary>
        int RemoveEquippedItem(IItem item, EquipmentSlot preferredSlot = EquipmentSlot.NoSlot);

        /// <summary>Unequips every current item, clears equipment, and publishes once when storage changes.</summary>
        void ClearEquipment();
    }
}
