using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Read-only item query view over storage owned by a domain container.</summary>
internal sealed class ReadOnlyItemContainer(ItemContainerStorage storage) : IReadOnlyItemContainer
{
    private readonly ItemContainerStorage _storage = storage;

    public int Capacity => _storage.Capacity;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IItem? this[int index] => _storage[index];
    public IItem? GetById(int id) => _storage.GetById(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool Contains(int id) => _storage.Contains(id);
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
