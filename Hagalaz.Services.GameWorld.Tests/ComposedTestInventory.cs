using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class ComposedTestInventory : IInventoryContainer
{
    public ItemContainer Items { get; }
    IItemContainer IInventoryContainer.Items => Items;
    public Action? OnUpdateAction { get; set; }

    public ComposedTestInventory(int capacity) =>
        Items = new ItemContainer(StorageType.Normal, capacity, _ => OnUpdateAction?.Invoke());

    public IItem? this[int index] => Items[index];
    public int Capacity => Items.Capacity;
    public IEnumerator<IItem?> GetEnumerator() => Items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    public bool DropItem(IItem item) => false;
}
