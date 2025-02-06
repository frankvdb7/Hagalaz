using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEquipmentContainer : IEnumerable<IItem?>, IEnumerable, IContainer<IItem?>
    {
        /// <summary>
        /// The number of free slots.
        /// </summary>
        /// <value>The free slots.</value>
        int FreeSlots { get; }
        /// <summary>
        /// Gets the item by the specified array index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>Returns the Item object.</returns>
        IItem? this[EquipmentSlot index] { get; }
        /// <summary>
        /// Equips item to this character.
        /// </summary>
        /// <param name="item">Item in inventory.</param>
        /// <returns>True if item was equiped sucessfully.</returns>
        bool EquipItem(IItem item);
        /// <summary>
        /// UnEquips item to this character.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="toInventorySlot">To inventory slot.</param>
        /// <returns>True if item was unequiped sucessfully.</returns>
        bool UnEquipItem(IItem item, int toInventorySlot = -1);
        /// <summary>
        /// Get's slot of specific item instance.
        /// -1 if this container does not contain specific instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Int32.</returns>
        EquipmentSlot GetInstanceSlot(IItem instance);
        /// <summary>
        /// Replaces the specific slot with the item.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="item"></param>
        void Replace(EquipmentSlot slot, IItem item);
        /// <summary>
        /// Removes a certain amount of Item objects from the container.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <param name="preferredSlot">Preferred slot for removal.</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        /// <returns>
        /// Returns the number of Item objects removed from the container.
        /// </returns>
        int Remove(IItem item, EquipmentSlot preferredSlot = EquipmentSlot.NoSlot, bool update = true);
        /// <summary>
        /// Gets an Item object by the specified id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// Returns the Item object instance; null if not found.
        /// </returns>
        IItem? GetById(int id);
        /// <summary>
        /// Add's a certain amount of item in specific slot.
        /// </summary>
        /// <param name="slot">Slot at which item should be added.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>If item was added.</returns>
        bool Add(EquipmentSlot slot, IItem item);
        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <param name="update">if set to <c>true</c> [update].</param>
        void Clear(bool update);
        /// <summary>
        /// Called when some or all items in the container has changed.
        /// </summary>
        void OnUpdate(HashSet<EquipmentSlot>? slots = null);
    }
}
