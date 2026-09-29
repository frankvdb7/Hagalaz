using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class ComposedTestInventory : IInventoryContainer, IItemContainerStorageOwner
{
    private readonly ItemContainerStorage _storage;

    ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
    void IItemContainerStorageOwner.PublishChanges(HashSet<int>? changedSlots) => OnUpdate(changedSlots);

    public Action? OnUpdateAction { get; set; }
    public ComposedTestInventory(int capacity) => _storage = new ItemContainerStorage(StorageType.Normal, capacity);
    public StorageType Type => _storage.Type;
    public int FreeSlots => _storage.FreeSlots;
    public int TakenSlots => _storage.TakenSlots;
    public bool DropItem(Hagalaz.Game.Abstractions.Model.Items.IItem item) => false;
    public void OnUpdate(HashSet<int>? changedSlots = null) => OnUpdateAction?.Invoke();
}
