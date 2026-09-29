using System.Collections;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters;

public partial class RewardContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);
}

public partial class FamiliarInventoryContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);
}

public partial class BankContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);
}

public partial class MoneyPouchContainer
{
    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);
}

public partial class EquipmentContainer
{
    IItem? IContainer<IItem?>.this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);
}
