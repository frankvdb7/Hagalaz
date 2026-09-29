using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Describes the operations and shape of an item container.</summary>
public interface IItemContainer : IContainer<IItem?>
{
    StorageType Type { get; }

    int FreeSlots { get; }

    int TakenSlots { get; }

    /// <summary>Adds an item and publishes after the storage mutation commits.</summary>
    bool Add(IItem item) => ItemContainerExtensions.Add(this, item);

    /// <summary>Adds an item into a specific slot and publishes after the mutation commits.</summary>
    bool Add(int slot, IItem item) => ItemContainerExtensions.Add(this, slot, item);

    void AddAndRemoveFrom(IItemContainer source) => ItemContainerExtensions.AddAndRemoveFrom(this, source);

    IItem? GetById(int id) => ItemContainerExtensions.GetById(this, id);

    int Remove(IItem item, int preferredSlot = -1, bool update = true) =>
        ItemContainerExtensions.Remove(this, item, preferredSlot, update);

    void Replace(int slot, IItem item) => ItemContainerExtensions.Replace(this, slot, item);

    void Swap(int fromSlot, int toSlot) => ItemContainerExtensions.Swap(this, fromSlot, toSlot);

    void Move(int fromSlot, int toSlot) => ItemContainerExtensions.Move(this, fromSlot, toSlot);

    bool AddRange(IEnumerable<IItem?> items) => ItemContainerExtensions.AddRange(this, items);

    bool Contains(int id, int count) => ItemContainerExtensions.Contains(this, id, count);

    bool Contains(int id) => ItemContainerExtensions.Contains(this, id);

    int GetCount(IItem item) => ItemContainerExtensions.GetCount(this, item);

    int GetCountById(int id) => ItemContainerExtensions.GetCountById(this, id);

    int GetInstanceSlot(IItem instance) => ItemContainerExtensions.GetInstanceSlot(this, instance);

    void Sort() => ItemContainerExtensions.Sort(this);

    int GetSlotByItem(IItem item, bool ignoreCount = true) => ItemContainerExtensions.GetSlotByItem(this, item, ignoreCount);

    bool HasSpaceFor(IItem item) => ItemContainerExtensions.HasSpaceFor(this, item);

    bool HasSpaceForRange(IEnumerable<IItem?> items) => ItemContainerExtensions.HasSpaceForRange(this, items);

    void Clear(bool update) => ItemContainerExtensions.Clear(this, update);
}
