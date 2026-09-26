using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Provides a foundational, abstract implementation for an item container,
    /// offering core logic for managing a collection of <see cref="IItem"/> objects.
    /// </summary>
    public abstract class BaseItemContainer : IItemContainer
    {
        private static long _nextMutationOrder;
        private readonly object _mutationLock = new();
        private readonly long _mutationOrder = Interlocked.Increment(ref _nextMutationOrder);

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
        /// Advances the container revision after a derived container applies a storage mutation.
        /// </summary>
        protected void AdvanceRevision() => _version++;

        /// <summary>
        /// The common container mutation boundary, exposed to trade containers
        /// for their existing multi-container settlement operation.
        /// </summary>
        protected object ContainerMutationLock => _mutationLock;

        /// <summary>
        /// Stable order for acquiring more than one container mutation boundary.
        /// </summary>
        protected long ContainerMutationOrder => _mutationOrder;

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
                        AddInitialItem(en.Current);
                    }
                }
            }
        }

        /// <summary>
        /// Moves an exact positive quantity between two base item containers.
        /// Both storage changes are committed under their mutation locks before
        /// either container is notified.
        /// </summary>
        public static bool TryTransfer(
            IItemContainer source,
            IItemContainer destination,
            IItem item,
            int count,
            int preferredSourceSlot = -1,
            int destinationSlot = -1,
            IItem? destinationItem = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(item);

            if (count <= 0 || ReferenceEquals(source, destination))
            {
                return false;
            }

            if (source is not BaseItemContainer sourceContainer || destination is not BaseItemContainer destinationContainer)
            {
                throw new ArgumentException("Both containers must use BaseItemContainer storage.");
            }

            if (destinationSlot < -1)
            {
                return false;
            }

            HashSet<int> sourceSlots = [];
            HashSet<int> destinationSlots = [];
            var transferred = false;

            WithOrderedMutationLocks(sourceContainer, destinationContainer, () =>
            {
                transferred = TryTransferLocked(sourceContainer, destinationContainer, item, count,
                    preferredSourceSlot, destinationSlot, destinationItem, sourceSlots, destinationSlots);
            });

            if (!transferred)
            {
                return false;
            }

            sourceContainer.NotifyTransferCommitted(sourceSlots);
            destinationContainer.NotifyTransferCommitted(destinationSlots);
            return true;
        }

        /// <summary>
        /// Notifies observers after a transfer has committed. Derived containers
        /// may retain their established post-commit observer behavior.
        /// </summary>
        protected virtual void NotifyTransferCommitted(HashSet<int> slots) => OnUpdate(slots);

        private static bool TryTransferLocked(
            BaseItemContainer source,
            BaseItemContainer destination,
            IItem item,
            int count,
            int preferredSourceSlot,
            int destinationSlot,
            IItem? destinationItem,
            HashSet<int> sourceSlots,
            HashSet<int> destinationSlots)
        {
            var removals = new List<(int Slot, int Count, IItem Item)>();
            if (!TryCreateRemovalPlan(source, item, count, preferredSourceSlot, removals))
            {
                return false;
            }

            var destinationTemplate = destinationItem ?? item;
            var incomingItems = CreateIncomingItems(source, destination, destinationTemplate, destinationItem != null, removals);
            var simulatedItems = new IItem?[destination.Items.Length];
            for (var i = 0; i < destination.Items.Length; i++)
            {
                simulatedItems[i] = destination.Items[i]?.Clone(destination.Items[i]!.Count);
            }

            var simulatedIncoming = incomingItems.Select(incoming => (IItem?)incoming.Item.Clone(incoming.Item.Count)).ToArray();
            var slotOrigins = new int[simulatedItems.Length];
            Array.Fill(slotOrigins, -1);

            if (destinationSlot >= 0)
            {
                if ((uint)destinationSlot >= (uint)simulatedItems.Length)
                {
                    return false;
                }

                foreach (var incoming in simulatedIncoming)
                {
                    if (incoming == null || !ApplyAddAt(simulatedItems, destinationSlot, incoming, destination.Type == StorageType.AlwaysStack))
                    {
                        return false;
                    }
                }

                if (simulatedItems[destinationSlot] != null)
                {
                    destinationSlots.Add(destinationSlot);
                    if (destination.Items[destinationSlot] == null)
                    {
                        slotOrigins[destinationSlot] = 0;
                    }
                }
            }
            else if (!destination.ApplyAddRange(simulatedItems, simulatedIncoming, destinationSlots, slotOrigins))
            {
                return false;
            }

            for (var slot = 0; slot < simulatedItems.Length; slot++)
            {
                if (simulatedItems[slot] != null && destination.Items[slot] == null &&
                    (uint)slotOrigins[slot] >= (uint)incomingItems.Count)
                {
                    return false;
                }
            }

            var committedDestination = (IItem?[])destination.Items.Clone();
            source.ApplyRemovalPlan(removals, sourceSlots);

            for (var slot = 0; slot < simulatedItems.Length; slot++)
            {
                if (simulatedItems[slot] is not { } simulatedItem)
                {
                    continue;
                }

                if (destination.Items[slot] is { } existingItem)
                {
                    existingItem.Count = simulatedItem.Count;
                    continue;
                }

                var incoming = incomingItems[slotOrigins[slot]];
                var newItem = incoming.Original ?? incoming.Item;
                newItem.Count = simulatedItem.Count;
                committedDestination[slot] = newItem;
            }

            destination.Items = committedDestination;
            source.AdvanceRevision();
            destination.AdvanceRevision();
            return true;
        }

        private static bool TryCreateRemovalPlan(
            BaseItemContainer source,
            IItem item,
            int count,
            int preferredSourceSlot,
            List<(int Slot, int Count, IItem Item)> removals)
        {
            long available = 0;
            foreach (var sourceItem in source.Items)
            {
                if (sourceItem != null && sourceItem.Count > 0 && sourceItem.Equals(item, true))
                {
                    available += sourceItem.Count;
                }
            }

            if (available < count)
            {
                return false;
            }

            var remaining = count;
            if ((uint)preferredSourceSlot < (uint)source.Items.Length &&
                source.Items[preferredSourceSlot] is { Count: > 0 } preferredItem && preferredItem.Equals(item, true))
            {
                AddRemoval(preferredSourceSlot, preferredItem);
            }

            for (var slot = 0; remaining > 0 && slot < source.Items.Length; slot++)
            {
                if (slot == preferredSourceSlot || source.Items[slot] is not { Count: > 0 } sourceItem || !sourceItem.Equals(item, true))
                {
                    continue;
                }

                AddRemoval(slot, sourceItem);
            }

            return remaining == 0;

            void AddRemoval(int slot, IItem sourceItem)
            {
                var removed = Math.Min(sourceItem.Count, remaining);
                removals.Add((slot, removed, sourceItem));
                remaining -= removed;
            }
        }

        /// <summary>
        /// Removes a complete requested quantity using the shared exact-removal
        /// behavior used by checked trade removal and cross-container transfer.
        /// The caller must hold this container's mutation lock.
        /// </summary>
        protected bool TryRemoveExactCore(IItem item, int count, int preferredSlot, out HashSet<int> slotsToUpdate)
        {
            slotsToUpdate = [];
            var removals = new List<(int Slot, int Count, IItem Item)>();
            if (!TryCreateRemovalPlan(this, item, count, preferredSlot, removals))
            {
                return false;
            }

            ApplyRemovalPlan(removals, slotsToUpdate);
            AdvanceRevision();
            return true;
        }

        private void ApplyRemovalPlan(IReadOnlyList<(int Slot, int Count, IItem Item)> removals, HashSet<int> slotsToUpdate)
        {
            foreach (var removal in removals)
            {
                var sourceItem = Items[removal.Slot]!;
                if (removal.Count == sourceItem.Count)
                {
                    if (CountToResetTo == -1)
                    {
                        Items[removal.Slot] = null;
                    }
                    else
                    {
                        sourceItem.Count = CountToResetTo;
                    }
                }
                else
                {
                    sourceItem.Count -= removal.Count;
                }

                slotsToUpdate.Add(removal.Slot);
            }
        }

        private static List<(IItem Item, IItem? Original)> CreateIncomingItems(
            BaseItemContainer source,
            BaseItemContainer destination,
            IItem destinationTemplate,
            bool isTransformed,
            IReadOnlyList<(int Slot, int Count, IItem Item)> removals)
        {
            var incoming = new List<(IItem Item, IItem? Original)>();
            var splitIntoUnits = destination.Type != StorageType.AlwaysStack &&
                                 !destinationTemplate.ItemDefinition.Stackable &&
                                 !destinationTemplate.ItemDefinition.Noted;

            foreach (var removal in removals)
            {
                var canMoveInstance = !isTransformed && removal.Count == removal.Item.Count && source.CountToResetTo == -1;
                if (splitIntoUnits)
                {
                    for (var unit = 0; unit < removal.Count; unit++)
                    {
                        incoming.Add((
                            (isTransformed ? destinationTemplate : removal.Item).Clone(1),
                            canMoveInstance && unit == 0 ? removal.Item : null));
                    }
                }
                else
                {
                    incoming.Add(((isTransformed ? destinationTemplate : removal.Item).Clone(removal.Count),
                        canMoveInstance ? removal.Item : null));
                }
            }

            return incoming;
        }

        private static bool ApplyAddAt(IItem?[] targetItems, int slot, IItem item, bool alwaysStack)
        {
            var slotItem = targetItems[slot];
            if (slotItem != null)
            {
                if (slotItem.Id != item.Id || !slotItem.ItemScript.CanStackItem(slotItem, item, alwaysStack))
                {
                    return false;
                }

                var total = slotItem.Count + (long)item.Count;
                if (total > int.MaxValue)
                {
                    return false;
                }

                slotItem.Count = (int)total;
                return true;
            }

            targetItems[slot] = item;
            return true;
        }

        private static void WithOrderedMutationLocks(BaseItemContainer firstContainer, BaseItemContainer secondContainer, Action mutation)
        {
            if (ReferenceEquals(firstContainer, secondContainer))
            {
                lock (firstContainer._mutationLock)
                {
                    mutation();
                }

                return;
            }

            var first = firstContainer._mutationOrder < secondContainer._mutationOrder ? firstContainer : secondContainer;
            var second = ReferenceEquals(first, firstContainer) ? secondContainer : firstContainer;
            lock (first._mutationLock)
            lock (second._mutationLock)
            {
                mutation();
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
            ArgumentNullException.ThrowIfNull(items);

            var simulatedItems = new IItem?[Items.Length];
            for (var i = 0; i < Items.Length; i++)
            {
                simulatedItems[i] = Items[i]?.Clone(Items[i]!.Count);
            }

            var simulatedRange = items.Select(item => item is null ? null : item.Clone(item.Count)).ToArray();
            return ApplyAddRange(simulatedItems, simulatedRange, []);
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

            lock (_mutationLock)
            {
                if (!ApplyAddAt(Items, slot, item, Type == StorageType.AlwaysStack))
                {
                    return false;
                }

                _version++;
            }

            OnUpdate([slot]);
            return true;
        }


        /// <summary>
        /// Adds an item to the container. It will either stack with an existing item or be placed in the first available free slot.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns><c>true</c> if the item was successfully added; otherwise, <c>false</c>.</returns>
        public virtual bool Add(IItem item)
        {
            HashSet<int> slots = [];
            lock (_mutationLock)
            {
                var stacked = false;
                for (var slot = 0; slot < Items.Length; slot++)
                {
                    var slotItem = Items[slot];

                    // If an identical item is located in this container.
                    if (slotItem != null && slotItem.Id == item.Id && slotItem.ItemScript.CanStackItem(slotItem, item, Type == StorageType.AlwaysStack))
                    {
                        var total = slotItem.Count + (long)item.Count;
                        if (total > int.MaxValue) return false;
                        slotItem.Count = (int)total;
                        slots.Add(slot);
                        _version++;
                        stacked = true;
                        break;
                    }
                }

                if (!stacked && (Type == StorageType.AlwaysStack || item.ItemDefinition.Stackable || item.ItemDefinition.Noted))
                {
                    // Not existing in container.
                    var slot = GetFreeSlot();
                    if (slot == -1) return false;
                    Items[slot] = item;
                    slots.Add(slot);
                    _version++;
                }
                else if (!stacked)
                {
                    if (FreeSlots < item.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < item.Count; i++)
                    {
                        var freeSlot = GetFreeSlot();
                        Items[freeSlot] = item.Clone();
                        Items[freeSlot]!.Count = 1;
                        slots.Add(freeSlot);
                    }

                    _version++;
                }
            }

            OnUpdate(slots);
            return true;
        }

        /// <summary>
        /// Adds a collection of items to this container.
        /// </summary>
        /// <param name="newItems">The collection of items to add.</param>
        /// <returns><c>true</c> if all items were added successfully; otherwise, <c>false</c>.</returns>
        /// <summary>
        /// Adds a collection of items to this container.
        /// </summary>
        /// <param name="newItems">The collection of items to add.</param>
        /// <returns><c>true</c> if all items were added successfully; otherwise, <c>false</c>.</returns>
        public virtual bool AddRange(IEnumerable<IItem?> newItems)
        {
            HashSet<int> slotsToUpdate;
            lock (_mutationLock)
            {
                if (!AddRangeCore(newItems, out slotsToUpdate))
                {
                    return false;
                }

                AdvanceRevision();
            }

            OnUpdate(slotsToUpdate);
            return true;
        }

        /// <summary>
        /// Applies the shared add-range storage implementation and returns the
        /// changed slots for the caller's notification boundary.
        /// </summary>
        protected bool AddRangeCore(IEnumerable<IItem?> newItems, out HashSet<int> slotsToUpdate)
        {
            ArgumentNullException.ThrowIfNull(newItems);

            var items = newItems.ToArray();
            if (!HasSpaceForRange(items))
            {
                slotsToUpdate = [];
                return false;
            }

            slotsToUpdate = new HashSet<int>();
            if (!ApplyAddRange(items, slotsToUpdate))
            {
                return false;
            }

            return true;
        }

        private void AddInitialItem(IItem item)
        {
            if (ApplyAddRange([item], new HashSet<int>()))
            {
                _version++;
            }
        }

        private bool ApplyAddRange(IEnumerable<IItem?> newItems, HashSet<int> slotsToUpdate) =>
            ApplyAddRange(Items, newItems.ToArray(), slotsToUpdate);

        private bool ApplyAddRange(IItem?[] targetItems, IReadOnlyList<IItem?> newItems, HashSet<int> slotsToUpdate, int[]? slotOrigins = null)
        {
            for (var inputIndex = 0; inputIndex < newItems.Count; inputIndex++)
            {
                var current = newItems[inputIndex];
                if (current == null)
                {
                    continue;
                }

                for (var i = 0; i < targetItems.Length; i++)
                {
                    var item = targetItems[i];
                    if (item == null || item.Id != current.Id || !item.ItemScript.CanStackItem(item, current, Type == StorageType.AlwaysStack))
                    {
                        continue;
                    }

                    var total = item.Count + (long)current.Count;
                    if (total > int.MaxValue)
                    {
                        return false;
                    }

                    item.Count = (int)total;
                    slotsToUpdate.Add(i);
                    goto end;
                }

                if (Type == StorageType.AlwaysStack || current.ItemDefinition.Stackable || current.ItemDefinition.Noted)
                {
                    var slot = GetFreeSlot(targetItems);
                    if (slot == -1)
                    {
                        return false;
                    }

                    targetItems[slot] = current;
                    slotsToUpdate.Add(slot);
                    if (slotOrigins != null)
                    {
                        slotOrigins[slot] = inputIndex;
                    }
                }
                else
                {
                    if (targetItems.Count(item => item == null) < current.Count)
                    {
                        return false;
                    }

                    for (var j = 0; j < current.Count; j++)
                    {
                        var freeSlot = GetFreeSlot(targetItems);
                        targetItems[freeSlot] = current.Clone();
                        targetItems[freeSlot]!.Count = 1;
                        slotsToUpdate.Add(freeSlot);
                        if (slotOrigins != null)
                        {
                            slotOrigins[freeSlot] = inputIndex;
                        }
                    }
                }

                end:
                {
                    continue;
                }
            }

            return true;
        }

        private int GetFreeSlot(IItem?[] targetItems) =>
            ReferenceEquals(targetItems, Items) ? GetFreeSlot() : Array.FindIndex(targetItems, item => item == null);

        /// <summary>
        /// Transfers all items from another container into this one.
        /// </summary>
        /// <param name="container">The source container from which to transfer items.</param>
        public virtual void AddAndRemoveFrom(IItemContainer container)
        {
            ArgumentNullException.ThrowIfNull(container);
            if (ReferenceEquals(this, container))
            {
                return;
            }

            for (var i = 0; i < container.Capacity; i++)
            {
                var containerItem = container[i];
                if (containerItem == null || containerItem.Count <= 0) continue;
                TryTransfer(container, this, containerItem, containerItem.Count, i);
            }
        }

        /// <summary>
        /// Removes a specified item from the container.
        /// </summary>
        /// <param name="item">The item to remove, including the amount to be removed.</param>
        /// <param name="preferredSlot">The preferred slot to remove from. If -1, any slot will be used.</param>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        /// <returns>The number of items actually removed.</returns>
        public virtual int Remove(IItem item, int preferredSlot = -1, bool update = true)
        {
            var slots = new HashSet<int>();
            int removed;
            lock (_mutationLock)
            {
                removed = ApplyRemove(item, preferredSlot, slots);
                if (removed > 0)
                {
                    AdvanceRevision();
                }
            }

            if (removed > 0 && update)
            {
                OnUpdate(slots);
            }

            return removed;
        }

        private int ApplyRemove(IItem item, int preferredSlot, HashSet<int> slotsToUpdate)
        {
            var removed = 0;
            if (Type == StorageType.AlwaysStack || item.ItemDefinition.Stackable || item.ItemDefinition.Noted)
            {
                var lastRemoved = 0;
                for (var slot = 0; slot < Items.Length; slot++)
                {
                    var slotItem = Items[slot];
                    if (slotItem == null || !slotItem.Equals(item))
                    {
                        continue;
                    }

                    var requestedFromSlot = item.Count;
                    var removedFromSlot = Math.Min(slotItem.Count, requestedFromSlot);
                    if (slotItem.Count > item.Count)
                    {
                        slotItem.Count -= item.Count;
                    }
                    else if (CountToResetTo != -1)
                    {
                        slotItem.Count = CountToResetTo;
                    }
                    else
                    {
                        Items[slot] = null;
                    }

                    slotsToUpdate.Add(slot);
                    lastRemoved = removedFromSlot;
                }

                return lastRemoved;
            }

            var slotIndex = GetSlotByItem(item);
            if (preferredSlot != -1)
            {
                var slotItem = Items[preferredSlot];
                if (slotItem != null && slotItem.Equals(item, true))
                {
                    slotIndex = preferredSlot;
                }
            }

            var toRemove = item.Count;
            while (toRemove > 0)
            {
                if (slotIndex == -1 && (slotIndex = GetSlotByItem(item)) == -1)
                {
                    break;
                }

                var slotItem = Items[slotIndex];
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Count > toRemove)
                {
                    slotItem.Count -= toRemove;
                    removed += toRemove;
                    slotsToUpdate.Add(slotIndex);
                    break;
                }

                removed += slotItem.Count;
                toRemove -= slotItem.Count;
                if (CountToResetTo != -1)
                {
                    slotItem.Count = CountToResetTo;
                }
                else
                {
                    Items[slotIndex] = null;
                }

                slotsToUpdate.Add(slotIndex);
                slotIndex = GetSlotByItem(item);
            }

            return removed;
        }

        /// <summary>
        /// Removes all items present in another container from this one.
        /// </summary>
        /// <param name="container">The container holding the items to remove.</param>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        public virtual void Remove(BaseItemContainer container, bool update = true)
        {
            ArgumentNullException.ThrowIfNull(container);
            if (ReferenceEquals(this, container))
            {
                return;
            }

            var slotsToUpdate = new HashSet<int>();
            WithOrderedMutationLocks(this, container, () =>
            {
                for (var i = 0; i < container.Capacity; i++)
                {
                    var containerItem = container.Items[i];
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
                                slotsToUpdate.Add(slot);
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

                if (slotsToUpdate.Count > 0)
                {
                    AdvanceRevision();
                }
            });

            if (update && slotsToUpdate.Count > 0) OnUpdate(slotsToUpdate);
        }

        /// <summary>
        /// Replaces the item at a specific slot with a new item, without any stacking or count checks.
        /// </summary>
        /// <param name="slot">The zero-based index of the slot to replace.</param>
        /// <param name="item">The new item to place in the slot. This cannot be null.</param>
        public virtual void Replace(int slot, IItem item)
        {
            lock (_mutationLock)
            {
                Items[slot] = item;
                _version++;
            }

            OnUpdate([slot]);
        }

        /// <summary>
        /// Moves an item from one slot to another, shifting existing items to fill the gap.
        /// </summary>
        /// <param name="fromSlot">The slot of the item to move.</param>
        /// <param name="toSlot">The destination slot.</param>
        public virtual void Move(int fromSlot, int toSlot)
        {
            lock (_mutationLock)
            {
                if ((uint)fromSlot >= (uint)Items.Length || (uint)toSlot >= (uint)Items.Length)
                {
                    return;
                }

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
                _version++;
            }

            OnUpdate();
        }

        /// <summary>
        /// Swaps the items in two specified slots.
        /// </summary>
        /// <param name="fromSlot">The first slot to swap.</param>
        /// <param name="toSlot">The second slot to swap.</param>
        public virtual void Swap(int fromSlot, int toSlot)
        {
            lock (_mutationLock)
            {
                var fromItem = Items[fromSlot];
                if (fromItem == null) return;

                Items[fromSlot] = Items[toSlot];
                Items[toSlot] = fromItem;
                _version++;
            }

            OnUpdate([fromSlot, toSlot]);
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
        public virtual void Sort()
        {
            lock (_mutationLock)
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

                _version++;
            }

            OnUpdate();
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
            lock (_mutationLock)
            {
                if (Items.Length <= 0)
                {
                    return;
                }

                Array.Clear(Items, 0, Items.Length);
                _version++;
            }

            if (update)
            {
                OnUpdate();
            }
        }

        /// <summary>
        /// Replaces the entire internal item array with a capacity-sized copy.
        /// </summary>
        /// <param name="items">The new array of items.</param>
        /// <param name="update">If set to <c>true</c>, the <see cref="OnUpdate"/> callback is invoked.</param>
        public virtual void SetItems(IItem[] items, bool update)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (items.Length != Capacity)
            {
                throw new ArgumentException("Item storage length must equal container capacity.", nameof(items));
            }

            lock (_mutationLock)
            {
                Items = (IItem?[])items.Clone();
                _version++;
            }

            if (update) OnUpdate();
        }

        /// <summary>
        /// Restores items into their exact physical slots without gameplay insertion.
        /// </summary>
        protected void RestoreItems(IEnumerable<(int Slot, IItem Item)> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            var restored = new IItem[Capacity];
            foreach (var (slot, item) in items)
            {
                if ((uint)slot >= (uint)Capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(items), $"Slot {slot} is outside the container capacity.");
                }

                if (restored[slot] != null)
                {
                    throw new ArgumentException($"Slot {slot} is duplicated in restored state.", nameof(items));
                }

                ArgumentNullException.ThrowIfNull(item);
                if (!IsValidRestoredCount(item.Count))
                {
                    throw new ArgumentOutOfRangeException(nameof(items), "Item count is invalid for this container.");
                }

                restored[slot] = item;
            }

            SetItems(restored, false);
        }

        /// <summary>
        /// Determines whether an item count is valid when restoring exact container state.
        /// </summary>
        protected virtual bool IsValidRestoredCount(int count) => count > 0;

        /// <summary>
        /// Enumerates non-empty items together with their physical slots.
        /// </summary>
        protected IEnumerable<(int Slot, IItem Item)> EnumerateOccupiedSlots()
        {
            var items = ToArray();
            for (var slot = 0; slot < items.Length; slot++)
            {
                if (items[slot] is { } item)
                {
                    yield return (slot, item);
                }
            }
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
