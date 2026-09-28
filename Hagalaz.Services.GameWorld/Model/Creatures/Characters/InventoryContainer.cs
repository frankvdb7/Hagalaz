using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters;

public class InventoryContainer : IInventoryContainer, IItemContainerStorageProvider,
    IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
{
    private readonly ICharacter _owner;
    private readonly IMapRegionService _mapRegionService;
    private readonly IGroundItemBuilder _groundItemBuilder;
    private readonly IItemBuilder _itemBuilder;
    private readonly ItemContainerStorage _storage;

    ItemContainerStorage IItemContainerStorageProvider.Storage => _storage;

    public InventoryContainer(ICharacter owner, int capacity, IMapRegionService mapRegionService,
        IGroundItemBuilder groundItemBuilder, IItemBuilder itemBuilder)
    {
        (_owner, _mapRegionService, _groundItemBuilder, _itemBuilder) =
            (owner, mapRegionService, groundItemBuilder, itemBuilder);
        _storage = new ItemContainerStorage(StorageType.Normal, capacity);
    }

    public IItem? this[int index] => _storage[index];
    public int Capacity => _storage.Capacity;
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;

    public bool Add(IItem item)
    {
        if (!_storage.TryAdd(item, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool Add(int slot, IItem item)
    {
        if (!_storage.TryAdd(slot, item, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public void AddAndRemoveFrom(IItemContainer container) => ItemContainerTransfer.AddAndRemoveFrom(this, container);
    public IItem? GetById(int id) => _storage.GetById(id);
    public int Remove(IItem item, int preferredSlot = -1, bool update = true)
    {
        var removed = _storage.Remove(item, preferredSlot, out var slots);
        if (removed > 0 && update) OnUpdate(slots);
        return removed;
    }
    public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
    public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
    public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(); }
    public bool AddRange(IEnumerable<IItem?> items)
    {
        if (!_storage.TryAddRange(items, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool Contains(int id, int count) => _storage.Contains(id, count);
    public bool Contains(int id) => _storage.Contains(id);
    public int GetCount(IItem item) => _storage.GetCount(item);
    public int GetCountById(int id) => _storage.GetCountById(id);
    public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
    public void Sort() { _storage.Sort(); OnUpdate(); }
    public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
    public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
    public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
    public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(); }
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool AddRangeForTrade(IEnumerable<IItem?> items)
    {
        if (!TryAddRangeForTradeStorage(items, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool TryAddRangeForTradeStorage(IEnumerable<IItem?> items, out HashSet<int> changedSlots) =>
        _storage.TryAddRange(items, out changedSlots);
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        if (!TryRemoveForTradeStorage(item, preferredSlot, out var slots)) return false;
        OnUpdate(slots);
        return true;
    }
    public bool TryRemoveForTradeStorage(IItem item, int preferredSlot, out HashSet<int> changedSlots) =>
        _storage.TryRemoveExact(item, preferredSlot, out changedSlots);

    public void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new InventoryChangedEvent(_owner, slots));

    public bool DropItem(IItem item)
    {
        var slot = GetInstanceSlot(item);
        if (slot == -1 || Remove(item, slot) < item.Count) return false;
        var groundItem = _groundItemBuilder.Create().WithItem(item).WithLocation(_owner.Location).WithOwner(_owner).Build();
        _mapRegionService.AddGroundItem(groundItem);
        return true;
    }

    public void Hydrate(IReadOnlyList<HydratedItemDto> inventory)
    {
        var items = new IItem?[Capacity];
        foreach (var entry in inventory)
        {
            if ((uint)entry.SlotId >= (uint)Capacity)
                throw new ArgumentOutOfRangeException(nameof(entry.SlotId));
            if (items[entry.SlotId] != null)
                throw new ArgumentException("Inventory contains a duplicate restored slot.", nameof(inventory));
            if (entry.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(entry.Count));
            items[entry.SlotId] = _itemBuilder.Create().WithId(entry.ItemId).WithCount(entry.Count)
                .WithExtraData(entry.ExtraData ?? string.Empty).Build();
        }
        _storage.ReplaceState(items);
    }

    public IReadOnlyList<HydratedItemDto> Dehydrate()
    {
        var entries = _storage.Select((item, slot) => (item, slot)).Where(entry => entry.item != null).ToArray();
        return entries.Select(entry => new HydratedItemDto(entry.item!.Id, entry.item.Count, entry.slot,
            entry.item.SerializeExtraData())).ToArray();
    }
}
