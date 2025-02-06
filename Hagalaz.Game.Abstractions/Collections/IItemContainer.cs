using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemContainer : IContainer<IItem?>
    {
        /// <summary>
        /// How the objects in this container are stored.
        /// </summary>
        /// <value>The type.</value>
        StorageType Type { get; }
        /// <summary>
        /// The number of free slots.
        /// </summary>
        /// <value>The free slots.</value>
        int FreeSlots { get; }
        /// <summary>
        /// The number of taken slots.
        /// </summary>
        /// <value>The taken slots.</value>
        int TakenSlots { get; }
        /// <summary>
        /// Adds a certain amount of Item objects to the container.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>Returns true if item was added successfully; False otherwise.</returns>
        bool Add(IItem item);
        /// <summary>
        /// Adds a certain amount of item in specific slot.
        /// </summary>
        /// <param name="slot">Slot at which item should be added.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>If item was added.</returns>
        bool Add(int slot, IItem item);
        /// <summary>
        /// Adds the and remove from.
        /// </summary>
        /// <param name="container">The container.</param>
        void AddAndRemoveFrom(IItemContainer container);
        /// <summary>
        /// Gets an Item object by the specified id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// Returns the Item object instance; null if not found.
        /// </returns>
        IItem? GetById(int id);
        /// <summary>
        /// Removes a certain amount of Item objects from the container.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <param name="preferredSlot">Preferred slot for removal.</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        /// <returns>
        /// Returns the number of Item objects removed from the container.
        /// </returns>
        int Remove(IItem item, int preferredSlot = -1, bool update = true);
        /// <summary>
        /// Replace's specific slot with specific item.
        /// Item cannot be null.
        /// This method does not check things such as stacking and counts.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="item">The item.</param>
        void Replace(int slot, IItem item);
        /// <summary>
        /// Swaps an item from one slot to another.
        /// </summary>
        /// <param name="fromSlot">The Item object's slot.</param>
        /// <param name="toSlot">The Item object's new slot.</param>
        void Swap(int fromSlot, int toSlot);
        /// <summary>
        /// Inserts an Item object to the specified slot.
        /// </summary>
        /// <param name="fromSlot">The slot the Item object is currently located.</param>
        /// <param name="toSlot">The slot to insert the Item object to.</param>
        void Move(int fromSlot, int toSlot);
        /// <summary>
        /// Adds an array of items to this container.
        /// </summary>
        /// <param name="newItems">The new items.</param>
        /// <returns>
        /// Returns true if successfully added all items; false otherwise.
        /// </returns>
        bool AddRange(IEnumerable<IItem?> newItems);
        /// <summary>
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <param name="count">The count.</param>
        /// <returns>Returns true if contained; false otherwise.</returns>
        bool Contains(int id, int count);
        /// <summary>
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <returns>Returns true if contained; false otherwise.</returns>
        bool Contains(int id);
        /// <summary>
        /// Gets the total number of a specified item in this container.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Return the total number of items of the same id in this container.</returns>
        int GetCount(IItem item);
        /// <summary>
        /// Gets the total number of a specified item in this container.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <returns>Return the total number of items of the same id in this container.</returns>
        int GetCountById(int id);
        /// <summary>
        /// Get's slot of specific item instance.
        /// -1 if this container does not contain specific instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Int32.</returns>
        int GetInstanceSlot(IItem instance);
        /// <summary>
        /// Sort's this container.
        /// </summary>
        void Sort();
        /// <summary>
        /// Gets the first slot that holds item which equals to the given item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="ignoreCount">if set to <c>true</c> [ignore count].</param>
        /// <returns>Returns the slot id; -1 if none.</returns>
        int GetSlotByItem(IItem item, bool ignoreCount = true);
        /// <summary>
        /// Get's if this container has space for specific items.
        /// </summary>
        /// <param name="item">Item for which space should be checked.</param>
        /// <returns>If this container has space for specific items.</returns>
        bool HasSpaceFor(IItem item);
        /// <summary>
        /// Get's if this container has space for specific items.
        /// </summary>
        /// <param name="items">Items for which space should be checked.</param>
        /// <returns>If this container has space for specific items.</returns>
        bool HasSpaceForRange(IEnumerable<IItem?> items);
        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <param name="update">if set to <c>true</c> [update].</param>
        void Clear(bool update);
        /// <summary>
        /// Called when some or all items in the container has changed.
        /// </summary>
        void OnUpdate(HashSet<int>? slots = null);
    }
}
