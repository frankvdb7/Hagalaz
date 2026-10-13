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
using Hagalaz.Services.GameWorld.Logic.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters;

public class InventoryContainer : IInventoryContainer, IItemTransactionSource,
    IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
{
    private readonly ICharacter _owner;
    private readonly IMapRegionService _mapRegionService;
    private readonly IGroundItemBuilder _groundItemBuilder;
    private readonly IItemBuilder _itemBuilder;
    private readonly ItemContainer _items;
    IReadOnlyList<ItemContainerMutationBoundary> IItemTransactionSource.Boundaries =>
        ((IItemTransactionSource)_items).Boundaries;
    public IReadOnlyItemContainer Items => _items;

    public InventoryContainer(ICharacter owner, int capacity, IMapRegionService mapRegionService,
        IGroundItemBuilder groundItemBuilder, IItemBuilder itemBuilder)
    {
        (_owner, _mapRegionService, _groundItemBuilder, _itemBuilder) =
            (owner, mapRegionService, groundItemBuilder, itemBuilder);
        _items = new ItemContainer(StorageType.Normal, capacity);
    }

    public bool AddItem(IItem item)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        if (!_items.Add(item)) return false;
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
        return true;
    }

    public bool AddItem(int slot, IItem item)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        if (!_items.Add(slot, item)) return false;
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
        return true;
    }

    public bool AddItems(IEnumerable<IItem?> items)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        if (!_items.AddRange(items)) return false;
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
        return true;
    }

    public int RemoveItem(IItem item, int preferredSlot = -1, bool publishChanges = true)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        var removed = _items.Remove(item, preferredSlot, publishChanges);
        if (removed > 0 && transaction != null) { transaction.Commit(); PublishChanges(); }
        return removed;
    }

    public bool TryRemoveItemExact(IItem item, int preferredSlot = -1)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        if (!_items.TryRemoveExact(item, preferredSlot)) return false;
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
        return true;
    }

    public bool TryTransferItemTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        using var transaction = _items.CurrentThreadTransaction == null
            ? ItemContainerTransaction.Begin(this, destination)
            : null;
        if (!_items.TryTransferTo(destination, item, count, preferredSourceSlot, destinationSlot, destinationItem)) return false;
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
        return true;
    }

    public bool TryTransferItemFrom(IItemContainer source, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var transaction = _items.CurrentThreadTransaction == null
            ? ItemContainerTransaction.Begin(this, source)
            : null;
        if (!source.TryTransferTo(_items, item, count, preferredSourceSlot, destinationSlot, destinationItem)) return false;
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
        return true;
    }

    public void ReplaceItem(int slot, IItem item)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        _items.Replace(slot, item);
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
    }

    public void SwapItems(int fromSlot, int toSlot)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        _items.Swap(fromSlot, toSlot);
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
    }

    public void ClearItems(bool publishChanges = true)
    {
        using var transaction = _items.CurrentThreadTransaction == null ? ItemContainerTransaction.Begin(this) : null;
        _items.Clear(publishChanges);
        if (transaction != null) { transaction.Commit(); PublishChanges(); }
    }

    public void PublishChanges()
    {
        var slots = _items.ChangeTracker.GetSlots();
        if (slots is { Count: 0 }) return;
        _owner.EventManager.SendEvent(new InventoryChangedEvent(_owner, slots));
    }

    public bool DropItem(IItem item)
    {
        var slot = Items.GetInstanceSlot(item);
        if (slot == -1) return false;
        var droppedItem = item.Clone(item.Count);
        if (!TryRemoveItemExact(item, slot)) return false;
        var groundItem = _groundItemBuilder.Create().WithItem(droppedItem).WithLocation(_owner.Location).WithOwner(_owner).Build();
        _mapRegionService.AddGroundItem(groundItem);
        return true;
    }

    public void Hydrate(IReadOnlyList<HydratedItemDto> inventory)
    {
        _items.RestoreItems(inventory.Select(entry => entry.ToStorageEntry(_itemBuilder)));
    }

    public IReadOnlyList<HydratedItemDto> Dehydrate()
    {
        var entries = _items.Select((item, slot) => (item, slot)).Where(entry => entry.item != null).ToArray();
        return entries.Select(entry => new HydratedItemDto(entry.item!.Id, entry.item.Count, entry.slot,
            entry.item.SerializeExtraData())).ToArray();
    }
}
