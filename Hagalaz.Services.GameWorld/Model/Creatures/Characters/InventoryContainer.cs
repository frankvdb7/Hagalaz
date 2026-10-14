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
    IItemContainerCompletionOwner,
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
        var storage = new ItemContainerStorage(StorageType.Normal, capacity);
        var mutations = new ItemContainerMutationBoundary(storage, this);
        _items = new ItemContainer(mutations);
    }

    public bool AddItem(IItem item)
    {
        return _items.Add(item);
    }

    public bool AddItem(int slot, IItem item)
    {
        return _items.Add(slot, item);
    }

    public bool AddItems(IEnumerable<IItem?> items)
    {
        return _items.AddRange(items);
    }

    public int RemoveItem(IItem item, int preferredSlot = -1, bool publishChanges = true)
    {
        return _items.Remove(item, preferredSlot, publishChanges);
    }

    public bool TryRemoveItemExact(IItem item, int preferredSlot = -1)
    {
        return _items.TryRemoveExact(item, preferredSlot);
    }

    public bool TryTransferItemTo(IItemContainer destination, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return _items.TryTransferTo(destination, item, count, preferredSourceSlot, destinationSlot, destinationItem);
    }

    public bool TryTransferItemFrom(IItemContainer source, IItem item, int count,
        int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.TryTransferTo(_items, item, count, preferredSourceSlot, destinationSlot, destinationItem);
    }

    public void ReplaceItem(int slot, IItem item)
    {
        _items.Replace(slot, item);
    }

    public void SwapItems(int fromSlot, int toSlot)
    {
        _items.Swap(fromSlot, toSlot);
    }

    public void ClearItems(bool publishChanges = true)
    {
        _items.Clear(publishChanges);
    }

    void IItemContainerCompletionOwner.PublishCommittedChanges()
    {
        var slots = _items.ChangeTracker.GetSlots();
        if (slots is { Count: 0 }) return;
        _owner.EventManager.SendEvent(new InventoryChangedEvent(_owner, slots));
    }

    void IItemContainerCompletionOwner.DiscardPendingCompletion() { }

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
