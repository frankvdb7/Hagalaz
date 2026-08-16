using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines a generic contract for a container that holds game items.
    /// This interface provides a comprehensive API for adding, removing, and managing items.
    /// </summary>
    public interface IItemContainer : IContainer<IItem?>
    {
        /// <summary>
        /// Gets the storage behavior of this container, such as how it handles item stacking.
        /// </summary>
        StorageType Type { get; }

        /// <summary>
        /// Gets the number of empty slots in the container.
        /// </summary>
        int FreeSlots { get; }

        /// <summary>
        /// Gets the number of occupied slots in the container.
        /// </summary>
        int TakenSlots { get; }

        /// <summary>
        /// Adds an item to the container, either by stacking it with an existing item or placing it in the first available slot.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns><c>true</c> if the item was added successfully; otherwise, <c>false</c>.</returns>
        bool Add(IItem item);

        /// <summary>
        /// Adds an item to a specific slot in the container.
        /// </summary>
        /// <param name="slot">The zero-based index of the slot to add the item to.</param>
        /// <param name="item">The item to add.</param>
        /// <returns><c>true</c> if the item was added successfully; otherwise, <c>false</c>.</returns>
        bool Add(int slot, IItem item);

        /// <summary>
        /// Transfers all items from another container into this one and removes them from the source.
        /// </summary>
        /// <param name="container">The source container from which to transfer items.</param>
        void AddAndRemoveFrom(IItemContainer container);

        /// <summary>
        /// Gets the first item in the container that has the specified ID.
        /// </summary>
        /// <param name="id">The item ID to search for.</param>
        /// <returns>The first <see cref="IItem"/> instance with the given ID, or <c>null</c> if not found.</returns>
        IItem? GetById(int id);

        /// <summary>
        /// Removes a specified item from the container.
        /// </summary>
        /// <param name="item">The item to remove, including the amount to be removed.</param>
        /// <param name="preferredSlot">The preferred slot to remove from. If -1, any slot containing the item will be used.</param>
        /// <param name="update">If set to <c>true</c>, an update callback is invoked.</param>
        /// <returns>The number of items actually removed.</returns>
        int Remove(IItem item, int preferredSlot = -1, bool update = true);

        /// <summary>
        /// Replaces the item at a specific slot with a new item, without any stacking or count checks.
        /// </summary>
        /// <param name="slot">The zero-based index of the slot to replace.</param>
        /// <param name="item">The new item to place in the slot. This cannot be null.</param>
        void Replace(int slot, IItem item);

        /// <summary>
        /// Swaps the items in two specified slots.
        /// </summary>
        /// <param name="fromSlot">The first slot to swap.</param>
        /// <param name="toSlot">The second slot to swap.</param>
        void Swap(int fromSlot, int toSlot);

        /// <summary>
        /// Moves an item from one slot to another, shifting existing items to fill the gap.
        /// </summary>
        /// <param name="fromSlot">The slot of the item to move.</param>
        /// <param name="toSlot">The destination slot.</param>
        void Move(int fromSlot, int toSlot);

        /// <summary>
        /// Adds a collection of items to this container.
        /// </summary>
        /// <param name="newItems">The collection of items to add.</param>
        /// <returns><c>true</c> if all items were added successfully; otherwise, <c>false</c>.</returns>
        bool AddRange(IEnumerable<IItem?> newItems);

        /// <summary>
        /// Determines whether the container holds at least a specified amount of an item with the given ID.
        /// </summary>
        /// <param name="id">The ID of the item to check for.</param>
        /// <param name="count">The minimum required amount.</param>
        /// <returns><c>true</c> if the container has at least the specified count of the item; otherwise, <c>false</c>.</returns>
        bool Contains(int id, int count);

        /// <summary>
        /// Determines whether the container holds at least one item with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the item to check for.</param>
        /// <returns><c>true</c> if an item with the ID is contained; otherwise, <c>false</c>.</returns>
        bool Contains(int id);

        /// <summary>
        /// Gets the total count of a specified item in this container, summing up all stacks.
        /// </summary>
        /// <param name="item">The item to count.</param>
        /// <returns>The total number of the specified item.</returns>
        int GetCount(IItem item);

        /// <summary>
        /// Gets the total count of an item by its ID in this container, summing up all stacks.
        /// </summary>
        /// <param name="id">The ID of the item to count.</param>
        /// <returns>The total number of items with the specified ID.</returns>
        int GetCountById(int id);

        /// <summary>
        /// Gets the slot of a specific item instance.
        /// </summary>
        /// <param name="instance">The exact item instance to find.</param>
        /// <returns>The zero-based index of the slot, or -1 if the specific instance is not found.</returns>
        int GetInstanceSlot(IItem instance);

        /// <summary>
        /// Sorts the container by moving all items to the beginning, removing any empty slots between them.
        /// </summary>
        void Sort();

        /// <summary>
        /// Gets the first slot that contains an item matching the specified item.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <param name="ignoreCount">If set to <c>true</c>, the item count is ignored during comparison.</param>
        /// <returns>The zero-based index of the first matching slot, or -1 if not found.</returns>
        int GetSlotByItem(IItem item, bool ignoreCount = true);

        /// <summary>
        /// Checks if the container has enough space to add a given item, considering stacking rules.
        /// </summary>
        /// <param name="item">The item to check space for.</param>
        /// <returns><c>true</c> if the item can be added; otherwise, <c>false</c>.</returns>
        bool HasSpaceFor(IItem item);

        /// <summary>
        /// Checks if the container has enough space to add a collection of items.
        /// </summary>
        /// <param name="items">The collection of items to check space for.</param>
        /// <returns><c>true</c> if all items can be added; otherwise, <c>false</c>.</returns>
        bool HasSpaceForRange(IEnumerable<IItem?> items);

        /// <summary>
        /// Clears all items from the container.
        /// </summary>
        /// <param name="update">If set to <c>true</c>, an update callback is invoked.</param>
        void Clear(bool update);

        /// <summary>
        /// A callback method invoked when the container's contents are updated.
        /// </summary>
        /// <param name="slots">A hash set of the specific slot indices that were changed. If null, a full update is assumed.</param>
        void OnUpdate(HashSet<int>? slots = null);
    }
}
