using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Provides a foundational, abstract implementation for an item container,
    /// offering core logic for managing a collection of <see cref="IItem"/> objects.
    /// </summary>
    public abstract class BaseItemContainer : IItemContainer
    {
        /// <summary>
        /// The internal array storing the item objects.
        /// </summary>
        protected IItem?[] Items;

        /// <summary>
        /// When an item's count is reduced to zero, if this value is not -1, the item's count
        /// is reset to this value instead of being removed from the container.
        /// </summary>
        protected int CountToResetTo = -1;

        private int _version;

        /// <summary>
        /// Gets the item at the specified index in the container.
        /// </summary>
        /// <param name="index">The zero-based index of the item to retrieve.</param>
        /// <returns>The <see cref="IItem"/> at the specified index, or <c>null</c> if the slot is empty.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of the valid range for the container.</exception>
        public IItem? this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Items.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return Items[index];
            }
        }

        /// <summary>
        /// Gets the total number of slots this container can hold.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Gets the storage behavior of this container, such as how it handles item stacking.
        /// </summary>
        public StorageType Type { get; }

        /// <summary>
        /// Gets the number of empty slots in the container.
        /// </summary>
        public int FreeSlots => Items.Count(t => t == null);

        /// <summary>
        /// Gets the number of occupied slots in the container.
        /// </summary>
        public int TakenSlots => Items.Count(t => t != null);

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemContainer"/> class with a specified storage type and capacity.
        /// </summary>
        /// <param name="type">The storage behavior type for the container.</param>
        /// <param name="capacity">The maximum number of slots the container can hold.</param>
        protected BaseItemContainer(StorageType type, int capacity)
        {
            Type = type;
            Capacity = capacity;
            Items = new IItem[capacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemContainer"/> class with a specified storage type, capacity, and an initial set of items.
        /// </summary>
        /// <param name="type">The storage behavior type for the container.</param>
        /// <param name="items">The initial collection of items to populate the container with.</param>
        /// <param name="capacity">The maximum number of slots the container can hold.</param>
        protected BaseItemContainer(StorageType type, IEnumerable<IItem> items, int capacity) : this(type, capacity)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items is ICollection<IItem?> c)
            {
                c.CopyTo(Items, 0);
            }
            else
            {
                using (var en = items.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }

        /// <summary>
        /// A callback method invoked when the container's contents are updated.
        /// This should be implemented by derived classes to handle state changes, such as refreshing a player's view.
        /// </summary>
        /// <param name="slots">A hash set of the specific slot indices that were changed. If null, a full update is assumed.</param>
        public abstract void OnUpdate(HashSet<int>? slots = null);

        /// <summary>
        /// Checks if the container has enough space to add a given item, considering stacking rules.
        /// </summary>
        /// <param name="item">The item to check space for.</param>
        /// <returns><c>true</c> if the item can be added; otherwise, <c>false</c>.</returns>
        public bool HasSpaceFor(IItem item)
        {
            foreach (var localItem in Items)
            {
                if (localItem != null && localItem.Id == item.Id && localItem.ItemScript.CanStackItem(localItem, item, Type == StorageType.AlwaysStack))
                {
                    long total = localItem.Count + (long)item.Count;
                    return total <= int.MaxValue;
                }
            }

            if (Type == StorageType.AlwaysStack || item.ItemDefinition.Stackable || item.ItemDefinition.Noted)
            {
                // Not existing in container.
                var slot = GetFreeSlot();
                return slot != -1;
            }

            return FreeSlots >= item.Count;
        }

        /// <summary>
        /// Checks if the container has enough space to add a collection of items.
        /// </summary>
        /// <param name="items">The collection of items to check space for.</param>
        /// <returns><c>true</c> if all items can be added; otherwise, <c>false</c>.</returns>
        public bool HasSpaceForRange(IEnumerable<IItem?> items)
        {
            var counts = new long[Capacity];
            var freeSlotsLeft = FreeSlots;
            var stacks = Type == StorageType.AlwaysStack;
            for (var i = 0; i < Capacity; i++)
            {
                var item = Items[i];
                if (item != null) counts[i] = item.Count;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                for (var t = 0; t < Items.Length; t++)
                {
                    var localItem = Items[t];
                    if (localItem != null && item.ItemScript.CanStackItem(item, localItem, stacks))
                    {
                        counts[t] += item.Count;
                        if (counts[t] > int.MaxValue) return false; // overflow
                        goto end;
                    }
                }

                if (--freeSlotsLeft < 0) return false;

                end:
                {
                    continue;
                }
            }

            return true;
        }


        /// <summary>
        /// Adds an item to a specific slot in the container. If the slot is occupied by a stackable item of the same type,
        /// it increases the count. If the slot is empty, it places the item there.
        /// </summary>
        /// <param name="slot">The zero-based index of the slot to add the item to.</param>
        /// <param name="item">The item to add.</param>
        /// <returns><c>true</c> if the item was successfully added; otherwise, <c>false</c>.</returns>
        public virtual bool Add(int slot, IItem item)
        {
            if (slot < 0 || slot >= Capacity)
            {
                return false;
            }

            var slotItem = Items[slot];

            if (slotItem != null && slotItem.Id == item.Id && slotItem.ItemScript.CanStackItem(slotItem, item, Type == StorageType.AlwaysStack))
            {
                var total = slotItem.Count + (long)item.Count;
                if (total > int.MaxValue) return false;
                slotItem.Count = (int)total;
                OnUpdate([slot]);
                _version++;
                return true;
            }

            if (slotItem != null)
            {
                return false;
            }
            Items[slot] = item;
            OnUpdate([slot]);
            _version++;
            return true;
        }


        /// <summary>
        /// Adds an item to the container. It will either stack with an existing item or be placed in the first available free slot.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns><c>true</c> if the item was successfully added; otherwise, <c>false</c>.</returns>
        public bool Add(IItem item)
        {
            for (var slot = 0; slot < Items.Length; slot++)
            {
                var slotItem = Items[slot];

                // If an identical item is located in this container.
                if (slotItem != null && slotItem.Id == item.Id && slotItem.ItemScript.CanStackItem(slotItem, item, Type == StorageType.AlwaysStack))
                {
                    var total = slotItem.Count + (long)item.Count;
                    if (total > int.MaxValue) return false;
                    slotItem.Count = (int)total;
                    OnUpdate([slot]);
                    _version++;
                    return true;
                }
            }

            if (Type == StorageType.AlwaysStack || item.ItemDefinition.Stackable || item.ItemDefinition.Noted)
            {
                // Not existing in container.
                var slot = GetFreeSlot();
                if (slot == -1) return false;
                Items[slot] = item;
                OnUpdate([slot]);
                _version++;
                return true;
            }

            if (FreeSlots < item.Count)
            {
                return false;
            }

            var slots = new HashSet<int>();
            for (var i = 0; i < item.Count; i++)
            {
                var freeSlot = GetFreeSlot();
                Items[freeSlot] = item.Clone();
                Items[freeSlot]!.Count = 1;
                slots.Add(freeSlot);
            }

            OnUpdate(slots);
            _version++;
            return true;
        }

        /// <summary>
        /// Adds a collection of items to this container.
        /// </summary>
        /// <param name="newItems">The collection of items to add.</param>
        /// <returns><c>true</c> if all items were added successfully; otherwise, <c>false</c>.</returns>
        public bool AddRange(IEnumerable<IItem?> newItems)
        {
            ArgumentNullException.ThrowIfNull(newItems);

            var slotsToUpdate = new HashSet<int>();

            using (var enumerator = newItems.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current == null)
                    {
                        continue;
                    }

                    for (var i = 0; i < Items.Length; i++)
                    {
                        // If an identical item is located in this container.
                        var item = Items[i];
                        if (item != null && item.Id == current.Id && item.ItemScript.CanStackItem(item, current, Type == StorageType.AlwaysStack))
                        {
                            var total = item.Count + (long)current.Count;
                            if (total > int.MaxValue)
                            {
                                return false;
                            }

                            item.Count = (int)total;
                            slotsToUpdate.Add(i);
                            goto end;
                        }
                    }

                    if (Type == StorageType.AlwaysStack || current.ItemDefinition.Stackable || current.ItemDefinition.Noted)
                    {
                        // Not existing in container.
                        var slot = GetFreeSlot();
                        if (slot == -1) return false;
                        Items[slot] = current.Clone();
                        slotsToUpdate.Add(slot);
                    }
                    else
                    {
                        if (FreeSlots < current.Count) return false;
                        for (var j = 0; j < current.Count; j++)
                        {
                            var freeSlot = GetFreeSlot();
                            Items[freeSlot] = current.Clone();
                            Items[freeSlot]!.Count = 1;
                            slotsToUpdate.Add(freeSlot);
                        }
                    }

                    end:
                    {
                        continue;
                    }
                }
            }

            OnUpdate(slotsToUpdate);
            _version++;
            return true;
        }

        /// <summary>
        /// Transfers all items from another container into this one.
        /// </summary>
        /// <param name="container">The source container from which to transfer items.</param>
        public void AddAndRemoveFrom(IItemContainer container)
        {
            var slotsToUpdate = new HashSet<int>();

            for (var i = 0; i < container.Capacity; i++)
            {
                var containerItem = container[i];
                if (containerItem == null) continue;

                for (short j = 0; j < Items.Length; j++)
                {
                    var item = Items[j];
                    // If an identical item is located in this container.
                    if (item != null && item.Id == containerItem.Id && item.ItemScript.CanStackItem(item, containerItem, Type == StorageType.AlwaysStack))
                    {
                        var total = item.Count + (long)containerItem.Count;
                        if (total > int.MaxValue)
                        {
                            continue;
                        }

                        var removed = container.Remove(containerItem, i);
                        item.Count += removed;
                        slotsToUpdate.Add(j);
                        goto end;
                    }
                }

                if (Type == StorageType.AlwaysStack || containerItem.ItemDefinition.Stackable || containerItem.ItemDefinition.Noted)
                {
                    // Not existing in container.
                    var slot = GetFreeSlot();
                    if (slot == -1) continue;
                    var toRemove = containerItem.Clone();
                    var removed = container.Remove(toRemove, i);
                    if (removed > 0)
                    {
                        toRemove.Count = removed;
                        Items[slot] = toRemove;
                        slotsToUpdate.Add(slot);
                    }

                    continue;
                }
                else
                {
                    if (FreeSlots < containerItem.Count) continue;
                    var toRemove = containerItem.Clone();
                    var removed = container.Remove(toRemove, i);
                    toRemove.Count = removed;
                    for (var j = 0; j < toRemove.Count; j++)
                    {
                        var freeSlot = GetFreeSlot();
                        Items[freeSlot] = toRemove;
                        Items[freeSlot]!.Count = 1;
                        slotsToUpdate.Add(freeSlot);
                    }

                    continue;
                }

                end:
                {
                    continue;
                }
            }

            OnUpdate(slotsToUpdate);
            _version++;
        }

        /// <summary>
        /// Removes a specified item from the container.
        /// </summary>
        /// <param name="item">The item to remove, including the amount to be removed.</param>
        /// <param name="preferredSlot">The preferred slot to remove from. If -1, any slot will be used.</param>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        /// <returns>The number of items actually removed.</returns>
        public int Remove(IItem item, int preferredSlot = -1, bool update = true)
        {
            var removed = 0;

            if (Type == StorageType.AlwaysStack || item.ItemDefinition.Stackable || item.ItemDefinition.Noted)
            {
                for (var slot = 0; slot < Items.Length; slot++)
                {
                    var slotItem = Items[slot];
                    if (slotItem == null || !slotItem.Equals(item)) continue;
                    if (slotItem.Count > item.Count)
                    {
                        removed = item.Count;
                        slotItem.Count -= item.Count;
                    }
                    else
                    {
                        removed = slotItem.Count;
                        if (CountToResetTo != -1)
                            slotItem.Count = CountToResetTo;
                        else
                            Items[slot] = null;
                    }

                    if (update)
                    {
                        OnUpdate([slot]);
                    }
                }
            }
            else
            {
                var slots = new HashSet<int>();
                var slot = GetSlotByItem(item);
                if (preferredSlot != -1)
                {
                    var slotItem = Items[preferredSlot];
                    if (slotItem != null && slotItem.Equals(item, true)) slot = preferredSlot;
                }

                var toRemove = item.Count;
                while (toRemove > 0)
                {
                    if (slot == -1 && (slot = GetSlotByItem(item)) == -1) break;
                    var slotItem = Items[slot];
                    if (slotItem == null)
                    {
                        continue;
                    }

                    if (slotItem.Count > toRemove)
                    {
                        removed += toRemove;
                        slotItem.Count -= toRemove;
                        break;
                    }

                    removed += slotItem.Count;
                    toRemove -= slotItem.Count;
                    if (CountToResetTo != -1)
                        slotItem.Count = CountToResetTo;
                    else
                        Items[slot] = null;

                    slots.Add(slot);
                    slot = GetSlotByItem(item);
                }

                if (update) OnUpdate(slots);
            }

            if (removed > 0)
            {
                _version++;
            }

            return removed;
        }

        /// <summary>
        /// Removes all items present in another container from this one.
        /// </summary>
        /// <param name="container">The container holding the items to remove.</param>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        public void Remove(BaseItemContainer container, bool update = true)
        {
            var slotsToUpdate = new HashSet<int>();
            for (var i = 0; i < container.Capacity; i++)
            {
                var containerItem = container[i];
                if (containerItem == null) continue;

                if (Type == StorageType.AlwaysStack || containerItem.ItemDefinition.Stackable || containerItem.ItemDefinition.Noted)
                {
                    for (var slot = 0; slot < Items.Length; slot++)
                    {
                        var slotItem = Items[slot];
                        if (slotItem == null || !slotItem.Equals(containerItem)) continue;
                        if (slotItem.Count > containerItem.Count)
                            slotItem.Count -= containerItem.Count;
                        else
                        {
                            if (CountToResetTo != -1)
                                slotItem.Count = CountToResetTo;
                            else
                                Items[slot] = null;
                        }

                        slotsToUpdate.Add(slot);
                    }
                }
                else
                {
                    var slot = GetSlotByItem(containerItem);
                    var toRemove = containerItem.Count;
                    while (toRemove > 0)
                    {
                        if (slot == -1 && (slot = GetSlotByItem(containerItem)) == -1) break;
                        var slotItem = Items[slot];
                        if (slotItem == null) continue;
                        if (slotItem.Count > toRemove)
                        {
                            slotItem.Count -= toRemove;
                            break;
                        }

                        toRemove -= slotItem.Count;
                        if (CountToResetTo != -1)
                            slotItem.Count = CountToResetTo;
                        else
                            Items[slot] = null;

                        slotsToUpdate.Add(slot);
                        slot = GetSlotByItem(containerItem);
                    }
                }
            }

            if (update) OnUpdate(slotsToUpdate);
        }

        /// <summary>
        /// Replaces the item at a specific slot with a new item, without any stacking or count checks.
        /// </summary>
        /// <param name="slot">The zero-based index of the slot to replace.</param>
        /// <param name="item">The new item to place in the slot. This cannot be null.</param>
        public virtual void Replace(int slot, IItem item)
        {
            Items[slot] = item;
            OnUpdate([slot]);
            _version++;
        }

        /// <summary>
        /// Moves an item from one slot to another, shifting existing items to fill the gap.
        /// </summary>
        /// <param name="fromSlot">The slot of the item to move.</param>
        /// <param name="toSlot">The destination slot.</param>
        public void Move(int fromSlot, int toSlot)
        {
            var fromItem = Items[fromSlot];
            if (fromItem == null) return;

            Items[fromSlot] = null;

            if (fromSlot > toSlot)
            {
                var shiftFrom = toSlot;
                var shiftTo = fromSlot;

                for (var i = toSlot + 1; i < fromSlot; i++)
                {
                    if (Items[i] != null)
                    {
                        continue;
                    }

                    shiftTo = i;
                    break;
                }

                var slice = new IItem[shiftTo - shiftFrom];
                Array.Copy(Items, shiftFrom, slice, 0, slice.Length);
                Array.Copy(slice, 0, Items, shiftFrom + 1, slice.Length);
            }
            else
            {
                var sliceStart = fromSlot + 1;
                var sliceEnd = toSlot;

                for (var i = sliceEnd - 1; i >= sliceStart; i--)
                {
                    if (Items[i] != null)
                    {
                        continue;
                    }

                    sliceStart = i;
                    break;
                }

                var slice = new IItem[sliceEnd - sliceStart + 1];
                Array.Copy(Items, sliceStart, slice, 0, slice.Length);
                Array.Copy(slice, 0, Items, sliceStart - 1, slice.Length);
            }

            Items[toSlot] = fromItem;
            OnUpdate();
            _version++;
        }

        /// <summary>
        /// Swaps the items in two specified slots.
        /// </summary>
        /// <param name="fromSlot">The first slot to swap.</param>
        /// <param name="toSlot">The second slot to swap.</param>
        public void Swap(int fromSlot, int toSlot)
        {
            var fromItem = Items[fromSlot];
            if (fromItem == null) return;

            Items[fromSlot] = Items[toSlot];
            Items[toSlot] = fromItem;

            OnUpdate([fromSlot, toSlot]);
            _version++;
        }

        /// <summary>
        /// Finds the first available empty slot in the container.
        /// </summary>
        /// <returns>The zero-based index of the first free slot, or -1 if the container is full.</returns>
        public virtual int GetFreeSlot() => Array.FindIndex(Items, item => item == null);

        /// <summary>
        /// Gets the first slot that contains an item matching the specified item.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <param name="ignoreCount">If set to <c>true</c>, the item count is ignored during comparison.</param>
        /// <returns>The zero-based index of the first matching slot, or -1 if not found.</returns>
        public int GetSlotByItem(IItem item, bool ignoreCount = true) => Array.FindIndex(Items, i => i != null && i.Equals(item, ignoreCount));

        /// <summary>
        /// Gets the first item in the container that has the specified ID.
        /// </summary>
        /// <param name="id">The item ID to search for.</param>
        /// <returns>The first <see cref="IItem"/> instance with the given ID, or <c>null</c> if not found.</returns>
        public IItem? GetById(int id) => Items.FirstOrDefault(item => item?.Id == id);

        /// <summary>
        /// Gets the total count of a specified item in this container, summing up all stacks.
        /// </summary>
        /// <param name="item">The item to count.</param>
        /// <returns>The total number of the specified item.</returns>
        public int GetCount(IItem item) => Items.OfType<IItem>().Where(tItem => tItem.Equals(item, true)).Sum(tItem => tItem.Count);

        /// <summary>
        /// Gets the total count of an item by its ID in this container, summing up all stacks.
        /// </summary>
        /// <param name="id">The ID of the item to count.</param>
        /// <returns>The total number of items with the specified ID.</returns>
        public int GetCountById(int id) =>
            (int)Items.OfType<IItem>().Where(item => item.Id == id).Aggregate<IItem, long>(0, (current, item) => current + item.Count);

        /// <summary>
        /// Sorts the container by moving all items to the beginning, removing any empty slots between them.
        /// </summary>
        public void Sort()
        {
            var baseWrite = 0;
            for (var i = 0; i < Items.Length; i++)
            {
                if (Items[i] == null)
                {
                    continue;
                }

                var item = Items[i];
                Items[i] = null;
                Items[baseWrite++] = item;
            }

            OnUpdate();
            _version++;
        }


        /// <summary>
        /// Determines whether the container holds at least one of the specified item.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="ignoreCount">If set to <c>true</c>, the item count is ignored during comparison.</param>
        /// <returns><c>true</c> if the item is contained; otherwise, <c>false</c>.</returns>
        public bool Contains(IItem item, bool ignoreCount = true) => GetSlotByItem(item, ignoreCount) != -1;

        /// <summary>
        /// Determines whether the container holds at least one item with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the item to check for.</param>
        /// <returns><c>true</c> if an item with the ID is contained; otherwise, <c>false</c>.</returns>
        public bool Contains(int id) => Items.Any(item => item?.Id == id);

        /// <summary>
        /// Determines whether the container holds at least a specified amount of an item with the given ID.
        /// </summary>
        /// <param name="id">The ID of the item to check for.</param>
        /// <param name="count">The minimum required amount.</param>
        /// <returns><c>true</c> if the container has at least the specified count of the item; otherwise, <c>false</c>.</returns>
        public virtual bool Contains(int id, int count) =>
            count <= Items.OfType<IItem>().Where(item => item.Id == id).Aggregate<IItem, long>(0, (current, item) => current + item.Count);

        /// <summary>
        /// Gets the slot of a specific item instance.
        /// </summary>
        /// <param name="instance">The exact item instance to find.</param>
        /// <returns>The zero-based index of the slot, or -1 if the specific instance is not found.</returns>
        public int GetInstanceSlot(IItem instance) => Array.IndexOf(Items, instance);

        /// <summary>
        /// Creates a new array containing all items in the container.
        /// </summary>
        /// <returns>A new array copy of the items.</returns>
        public IItem?[] ToArray() => Enumerable.ToArray(this);

        /// <summary>
        /// Clears all items from the container.
        /// </summary>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        public virtual void Clear(bool update)
        {
            if (Items.Length <= 0)
            {
                return;
            }

            Array.Clear(Items, 0, Items.Length);

            if (update)
            {
                OnUpdate();
            }

            _version++;
        }

        /// <summary>
        /// Replaces the entire internal item array with a new one.
        /// </summary>
        /// <param name="items">The new array of items.</param>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        public virtual void SetItems(IItem[] items, bool update)
        {
            Items = items;

            if (update) OnUpdate();
            _version++;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IItem?> GetEnumerator() => new ItemContainerEnumerator(this);

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => new ItemContainerEnumerator(this);

        /// <summary>
        /// A version-aware enumerator for the item container to prevent modification during enumeration.
        /// </summary>
        [Serializable]
        private struct ItemContainerEnumerator : IEnumerator<IItem?>, IEnumerator
        {
            private readonly BaseItemContainer _container;

            private int _index;


            private readonly int _version;
            private IItem? _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="ItemContainerEnumerator"/> struct.
            /// </summary>
            /// <param name="container">The container to enumerate.</param>
            internal ItemContainerEnumerator(BaseItemContainer container)
            {
                _container = container;
                _index = 0;
                _version = container._version;
                _current = default;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced; otherwise, <c>false</c>.</returns>
            public bool MoveNext()
            {
                var container = _container;
                if (_version == container._version && (uint)_index < (uint)_container.Items.Length)
                {
                    _current = container.Items[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            /// <summary>
            /// Handles the rare case for MoveNext, primarily for checking version mismatches.
            /// </summary>
            private bool MoveNextRare()
            {
                if (_version != _container._version)
                {
                    throw new InvalidOperationException("Version mismatch");
                }

                _index = _container.Items.Length + 1;
                _current = default;
                return false;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public IItem? Current => _current;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _container.Items.Length + 1)
                    {
                        throw new InvalidOperationException("Index out of bounds.");
                    }

                    return Current;
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            void IEnumerator.Reset()
            {
                if (_version != _container._version)
                {
                    throw new InvalidOperationException("Version mismatch");
                }

                _index = 0;
                _current = default;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose() { }
        }
    }
}