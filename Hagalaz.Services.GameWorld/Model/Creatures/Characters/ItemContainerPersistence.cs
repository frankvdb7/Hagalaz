using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters;

internal static class ItemContainerPersistence
{
    internal static IEnumerable<(int Slot, IItem Item)> Build(IReadOnlyList<HydratedItemDto> items, IItemBuilder builder) =>
        items.Select(item => (item.SlotId, BuildItem(item.ItemId, item.Count, item.ExtraData, builder)));

    internal static IEnumerable<(int Slot, IItem Item)> Build(IReadOnlyList<HydratedItem> items, IItemBuilder builder) =>
        items.Select(item => (item.SlotId, BuildItem(item.ItemId, item.Count, item.ExtraData, builder)));

    internal static IReadOnlyList<HydratedItemDto> Dehydrate(BaseItemContainer container)
    {
        var items = new List<HydratedItemDto>();
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            if (container[slot] is { } item)
            {
                items.Add(new HydratedItemDto(item.Id, item.Count, slot, item.SerializeExtraData()));
            }
        }

        return items;
    }

    private static IItem BuildItem(int id, int count, string? extraData, IItemBuilder builder)
    {
        var item = builder.Create().WithId(id).WithCount(count);
        if (!string.IsNullOrEmpty(extraData))
        {
            item.WithExtraData(extraData);
        }

        return item.Build();
    }
}
