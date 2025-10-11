using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a character's equipment container, which manages the items a character is currently wearing.
    /// </summary>
    public interface IEquipmentContainer : IEnumerable<IItem?>, IEnumerable, IContainer<IItem?>
    {
        /// <summary>
        /// Gets the number of empty equipment slots.
        /// </summary>
        int FreeSlots { get; }

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

        /// <summary>
        /// Gets the equipment slot of a specific item instance currently worn by the character.
        /// </summary>
        /// <param name="instance">The exact item instance to find.</param>
        /// <returns>The <see cref="EquipmentSlot"/> where the item is equipped, or <see cref="EquipmentSlot.NoSlot"/> if not found.</returns>
        EquipmentSlot GetInstanceSlot(IItem instance);

        /// <summary>
        /// Replaces the item in a specific slot with a new item, without any validation.
        /// </summary>
        /// <param name="slot">The equipment slot to replace.</param>
        /// <param name="item">The new item to place in the slot.</param>
        void Replace(EquipmentSlot slot, IItem item);

        /// <summary>
        /// Removes a specified item from the equipment container.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <param name="preferredSlot">The preferred slot to remove the item from.</param>
        /// <param name="update">If set to <c>true</c>, an update callback is invoked.</param>
        /// <returns>The number of items actually removed.</returns>
        int Remove(IItem item, EquipmentSlot preferredSlot = EquipmentSlot.NoSlot, bool update = true);

        /// <summary>
        /// Gets the first equipped item that matches the specified ID.
        /// </summary>
        /// <param name="id">The item ID to search for.</param>
        /// <returns>The first equipped <see cref="IItem"/> instance with the given ID, or <c>null</c> if not found.</returns>
        IItem? GetById(int id);

        /// <summary>
        /// Adds an item to a specific equipment slot. This is typically used for direct manipulation rather than standard equipping logic.
        /// </summary>
        /// <param name="slot">The equipment slot where the item should be added.</param>
        /// <param name="item">The item to add.</param>
        /// <returns><c>true</c> if the item was added successfully; otherwise, <c>false</c>.</returns>
        bool Add(EquipmentSlot slot, IItem item);

        /// <summary>
        /// Clears all items from the equipment container.
        /// </summary>
        /// <param name="update">If set to <c>true</c>, an update callback is invoked.</param>
        void Clear(bool update);

        /// <summary>
        /// A callback method invoked when the equipment container's contents are updated.
        /// </summary>
        /// <param name="slots">A hash set of the specific equipment slots that were changed. If null, a full update is assumed.</param>
        void OnUpdate(HashSet<EquipmentSlot>? slots = null);
    }
}
