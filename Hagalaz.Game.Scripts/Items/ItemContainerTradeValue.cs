using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Game.Scripts.Items;

internal static class ItemContainerTradeValue
{
    public static long Calculate(IItemContainer container)
    {
        long total = 0;
        for (var slot = 0; slot < container.Capacity; slot++)
        {
            if (container[slot] is not { } item) continue;
            if ((ulong)total + (ulong)item.ItemDefinition.TradeValue * (ulong)item.Count > int.MaxValue) return -1;
            total += item.ItemDefinition.TradeValue * item.Count;
        }

        return total;
    }
}
