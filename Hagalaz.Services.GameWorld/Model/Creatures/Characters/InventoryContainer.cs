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

public class InventoryContainer : IInventoryContainer, IItemContainerStorageOwner,
    IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
{
    private readonly ICharacter _owner;
    private readonly IMapRegionService _mapRegionService;
    private readonly IGroundItemBuilder _groundItemBuilder;
    private readonly IItemBuilder _itemBuilder;
    private readonly ItemContainerStorage _storage;

    ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);

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
    public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new InventoryChangedEvent(_owner, slots));

    public bool DropItem(IItem item)
    {
        var slot = this.GetInstanceSlot(item);
        if (slot == -1 || this.Remove(item, slot) < item.Count) return false;
        var groundItem = _groundItemBuilder.Create().WithItem(item).WithLocation(_owner.Location).WithOwner(_owner).Build();
        _mapRegionService.AddGroundItem(groundItem);
        return true;
    }

    public void Hydrate(IReadOnlyList<HydratedItemDto> inventory)
    {
        _storage.RestoreItems(inventory.Select(entry => entry.ToStorageEntry(_itemBuilder)));
    }

    public IReadOnlyList<HydratedItemDto> Dehydrate()
    {
        var entries = _storage.Select((item, slot) => (item, slot)).Where(entry => entry.item != null).ToArray();
        return entries.Select(entry => new HydratedItemDto(entry.item!.Id, entry.item.Count, entry.slot,
            entry.item.SerializeExtraData())).ToArray();
    }
}
