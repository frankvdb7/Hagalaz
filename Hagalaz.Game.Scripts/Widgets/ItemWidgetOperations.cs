using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets;

internal static class ItemWidgetOperations
{
    public static bool TryGetItem(IContainer<IItem?> items, int itemId, int slot, [NotNullWhen(true)] out IItem? item)
    {
        item = null;
        if (slot < 0 || slot >= items.Capacity)
        {
            return false;
        }

        item = items[slot];
        return item != null && item.Id == itemId;
    }

    public static int GetCommonAmount(ComponentClickType clickType) => clickType switch
    {
        ComponentClickType.LeftClick => 1,
        ComponentClickType.Option2Click => 5,
        ComponentClickType.Option3Click => 10,
        _ => 0
    };
}
