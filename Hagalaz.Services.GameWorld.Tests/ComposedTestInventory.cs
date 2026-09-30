using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class ComposedTestInventory : IInventoryContainer
{
    public ItemContainer Items { get; }
    public Action? OnUpdateAction { get; set; }

    public ComposedTestInventory(int capacity) =>
        Items = new ItemContainer(StorageType.Normal, capacity, _ => OnUpdateAction?.Invoke());

    public bool DropItem(IItem item) => false;
}
