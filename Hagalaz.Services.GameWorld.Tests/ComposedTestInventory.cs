using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class ComposedTestInventory : IInventoryContainer, IItemContainerStorageProvider
{
    private readonly ItemContainerStorage _storage;

    ItemContainerStorage IItemContainerStorageProvider.Storage => _storage;
    void IItemContainerStorageProvider.PublishChanges(HashSet<int>? changedSlots) => OnUpdate(changedSlots);

    public Action? OnUpdateAction { get; set; }
    public ComposedTestInventory(int capacity) => _storage = new ItemContainerStorage(StorageType.Normal, capacity);
    public bool DropItem(Hagalaz.Game.Abstractions.Model.Items.IItem item) => false;
    public void OnUpdate(HashSet<int>? changedSlots = null) => OnUpdateAction?.Invoke();
}
