using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Represents a RSItem container.
    /// Base by Graham.
    /// </summary>
    public abstract class BaseItemContainer : IItemContainer
    {
        /// <summary>
        /// An array of <code>Hagalaz.Game.Model.RSItems.Item</code> objects.
        /// </summary>
        protected IItem?[] Items;

        /// <summary>
        /// The count to reset to.
        /// </summary>
        protected int CountToResetTo = -1;

        private int _version;

        /// <summary>
        /// Gets the item by the specified array index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// Returns the Item object.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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
        /// The capacity for the amount of objects the internal array can hold.
        /// </summary>
        /// <value>The capacity.</value>
        public int Capacity { get; }

        /// <summary>
        /// How the items in this container are stored.
        /// </summary>
        /// <value>The type.</value>
        public StorageType Type { get; }

        /// <summary>
        /// The number of free slots.
        /// </summary>
        /// <value>The free slots.</value>
        public int FreeSlots => Items.Count(t => t == null);

        /// <summary>
        /// The number of taken slots.
        /// </summary>
        /// <value>The taken slots.</value>
        public int TakenSlots => Items.Count(t => t != null);

        /// <summary>
        /// Constructs a new container with the specified capacity limit.
        /// </summary>
        /// <param name="type">The type of storage.</param>
        /// <param name="capacity">The capacity for this container.</param>
        protected BaseItemContainer(StorageType type, int capacity)
        {
            Type = type;
            Capacity = capacity;
            Items = new IItem[capacity];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="items"></param>
        /// <param name="capacity"></param>
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
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public abstract void OnUpdate(HashSet<int>? slots = null);

        /// <summary>
        /// Get's if this container has space for specific items.
        /// </summary>
        /// <param name="item">Item for which space should be checked.</param>
        /// <returns>If this container has space for specific items.</returns>
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
        /// Get's if this container has space for specific items.
        /// </summary>
        /// <param name="items">Items for which space should be checked.</param>
        /// <returns>If this container has space for specific items.</returns>
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
        /// Adds a certain amount of item in specific slot.
        /// </summary>
        /// <param name="slot">Slot at which item should be added.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>If item was added.</returns>
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
        /// Adds a certain amount of Item objects to the container.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>Returns true if item was added successfully; False otherwise.</returns>
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
        /// Adds an array of items to this container.
        /// </summary>
        /// <param name="newItems">The new items.</param>
        /// <returns>
        /// Returns true if successfully added all items; false otherwise.
        /// </returns>
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
        /// Adds the and remove from.
        /// </summary>
        /// <param name="container">The container.</param>
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
        /// Removes a certain amount of Item objects from the container.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <param name="preferredSlot">Preferred slot for removal.</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        /// <returns>
        /// Returns the number of Item objects removed from the container.
        /// </returns>
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
        /// Removes the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
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
        /// Replace's specific slot with specific item.
        /// Item cannot be null.
        /// This method does not check things such as stacking and counts.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="item">The item.</param>
        public virtual void Replace(int slot, IItem item)
        {
            Items[slot] = item;
            OnUpdate([slot]);
            _version++;
        }

        /// <summary>
        /// Moves an Item from a slot to the specified slot.
        /// </summary>
        /// <param name="fromSlot">The slot the Item object is currently located.</param>
        /// <param name="toSlot">The slot to insert the Item object to.</param>
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
        /// Swaps an item from one slot to another.
        /// </summary>
        /// <param name="fromSlot">The Item object's slot.</param>
        /// <param name="toSlot">The Item object's new slot.</param>
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
        /// Attempts to find a free slot in the container.
        /// </summary>
        /// <returns>Returns the availible slot id; -1 if none.</returns>
        public virtual int GetFreeSlot() => Array.FindIndex(Items, item => item == null);

        /// <summary>
        /// Gets the first slot that holds item which equals to the given item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="ignoreCount">if set to <c>true</c> [ignore count].</param>
        /// <returns>Returns the slot id; -1 if none.</returns>
        public int GetSlotByItem(IItem item, bool ignoreCount = true) => Array.FindIndex(Items, i => i != null && i.Equals(item, ignoreCount));

        /// <summary>
        /// Gets an Item object by the specified id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// Returns the Item object instance; null if not found.
        /// </returns>
        public IItem? GetById(int id) => Items.FirstOrDefault(item => item?.Id == id);

        /// <summary>
        /// Gets the total number of a specified item in this container.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Return the total number of items of the same id in this container.</returns>
        public int GetCount(IItem item) => Items.OfType<IItem>().Where(tItem => tItem.Equals(item, true)).Sum(tItem => tItem.Count);

        /// <summary>
        /// Gets the total number of a specified item in this container.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <returns>Return the total number of items of the same id in this container.</returns>
        public int GetCountById(int id) =>
            (int)Items.OfType<IItem>().Where(item => item.Id == id).Aggregate<IItem, long>(0, (current, item) => current + item.Count);

        /// <summary>
        /// Sort's this container.
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
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="ignoreCount">if set to <c>true</c> [ignore count].</param>
        /// <returns>Returns true if contained; false otherwise.</returns>
        public bool Contains(IItem item, bool ignoreCount = true) => GetSlotByItem(item, ignoreCount) != -1;

        /// <summary>
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <returns>Returns true if contained; false otherwise.</returns>
        public bool Contains(int id) => Items.Any(item => item?.Id == id);

        /// <summary>
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <param name="count">The count.</param>
        /// <returns>Returns true if contained; false otherwise.</returns>
        public virtual bool Contains(int id, int count) =>
            count <= Items.OfType<IItem>().Where(item => item.Id == id).Aggregate<IItem, long>(0, (current, item) => current + item.Count);

        /// <summary>
        /// Get's slot of specific item instance.
        /// -1 if this container does not contain specific instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Int32.</returns>
        public int GetInstanceSlot(IItem instance) => Array.IndexOf(Items, instance);

        /// <summary>
        /// Creates a copy of the internal container array.
        /// </summary>
        /// <returns>
        /// Returns an array of Item objects.
        /// </returns>
        public IItem?[] ToArray() => Enumerable.ToArray(this);

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <param name="update">if set to <c>true</c> [update].</param>
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
        /// Sets the internal items array with the given array.
        /// </summary>
        /// <param name="items">An array of Item objects.</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        public virtual void SetItems(IItem[] items, bool update)
        {
            Items = items;

            if (update) OnUpdate();
            _version++;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<IItem?> GetEnumerator() => new ItemContainerEnumerator(this);

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => new ItemContainerEnumerator(this);

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="IEnumerator{Item}" />
        /// <seealso cref="IEnumerator" />
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
            /// <param name="container">The container.</param>
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
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
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
            /// Moves the next rare.
            /// </summary>
            /// <returns></returns>
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
            /// Gets the current.
            /// </summary>
            /// <value>
            /// The current.
            /// </value>
            public IItem? Current => _current;

            /// <summary>
            /// Gets the current.
            /// </summary>
            /// <value>
            /// The current.
            /// </value>
            /// <exception cref="InvalidOperationException">Index out of bounds.</exception>
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