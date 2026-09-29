using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Provides item operations for containers backed by composed storage.</summary>
public static class ItemContainerExtensions
{
    public static bool Add(this IItemContainer container, IItem item)
    {
        var provider = GetProvider(container);
        if (!provider.Storage.TryAdd(item, out var changedSlots)) return false;
        provider.PublishChanges(changedSlots);
        return true;
    }

    public static bool Add(this IItemContainer container, int slot, IItem item)
    {
        var provider = GetProvider(container);
        if (!provider.Storage.TryAdd(slot, item, out var changedSlots)) return false;
        provider.PublishChanges(changedSlots);
        return true;
    }

    public static void AddAndRemoveFrom(this IItemContainer container, IItemContainer source) =>
        ItemContainerTransfer.AddAndRemoveFrom(container, source);

    public static IItem? GetById(this IItemContainer container, int id) => GetProvider(container).Storage.GetById(id);

    public static int Remove(this IItemContainer container, IItem item, int preferredSlot = -1, bool update = true)
    {
        var provider = GetProvider(container);
        var removed = provider.Storage.Remove(item, preferredSlot, out var changedSlots);
        if (removed > 0 && update) provider.PublishChanges(changedSlots);
        return removed;
    }

    public static void Replace(this IItemContainer container, int slot, IItem item)
    {
        var provider = GetProvider(container);
        provider.Storage.Replace(slot, item);
        provider.PublishChanges([slot]);
    }

    public static void Swap(this IItemContainer container, int fromSlot, int toSlot)
    {
        var provider = GetProvider(container);
        if (provider.Storage.Swap(fromSlot, toSlot)) provider.PublishChanges([fromSlot, toSlot]);
    }

    public static void Move(this IItemContainer container, int fromSlot, int toSlot)
    {
        var provider = GetProvider(container);
        if (provider.Storage.Move(fromSlot, toSlot)) provider.PublishChanges(null);
    }

    public static bool AddRange(this IItemContainer container, IEnumerable<IItem?> items)
    {
        var provider = GetProvider(container);
        if (!provider.Storage.TryAddRange(items, out var changedSlots)) return false;
        provider.PublishChanges(changedSlots);
        return true;
    }

    public static bool Contains(this IItemContainer container, int id, int count) => GetProvider(container).Storage.Contains(id, count);

    public static bool Contains(this IItemContainer container, int id) => GetProvider(container).Storage.Contains(id);

    public static int GetCount(this IItemContainer container, IItem item) => GetProvider(container).Storage.GetCount(item);

    public static int GetCountById(this IItemContainer container, int id) => GetProvider(container).Storage.GetCountById(id);

    public static int GetInstanceSlot(this IItemContainer container, IItem instance) => GetProvider(container).Storage.GetInstanceSlot(instance);

    public static void Sort(this IItemContainer container)
    {
        var provider = GetProvider(container);
        provider.Storage.Sort();
        provider.PublishChanges(null);
    }

    public static int GetSlotByItem(this IItemContainer container, IItem item, bool ignoreCount = true) =>
        GetProvider(container).Storage.GetSlotByItem(item, ignoreCount);

    public static bool HasSpaceFor(this IItemContainer container, IItem item) => GetProvider(container).Storage.HasSpaceFor(item);

    public static bool HasSpaceForRange(this IItemContainer container, IEnumerable<IItem?> items) =>
        GetProvider(container).Storage.HasSpaceForRange(items);

    public static void Clear(this IItemContainer container, bool update)
    {
        var provider = GetProvider(container);
        if (provider.Storage.Clear() && update) provider.PublishChanges(null);
    }

    public static void PublishChanges(this IItemContainer container, HashSet<int>? changedSlots = null) =>
        GetProvider(container).PublishChanges(changedSlots);

    public static bool AddRangeForTrade(this ITradeItemContainer container, IEnumerable<IItem?> items)
    {
        if (!container.TryAddRangeForTradeStorage(items, out var changedSlots)) return false;
        GetProvider(container).PublishChanges(changedSlots);
        return true;
    }

    public static bool TryAddRangeForTradeStorage(
        this ITradeItemContainer container,
        IEnumerable<IItem?> items,
        out HashSet<int> changedSlots) =>
        GetProvider(container).Storage.TryAddRange(items, out changedSlots);

    public static bool RemoveForTrade(this ITradeItemContainer container, IItem item, int preferredSlot = -1)
    {
        if (!container.TryRemoveForTradeStorage(item, preferredSlot, out var changedSlots)) return false;
        GetProvider(container).PublishChanges(changedSlots);
        return true;
    }

    public static bool TryRemoveForTradeStorage(
        this ITradeItemContainer container,
        IItem item,
        int preferredSlot,
        out HashSet<int> changedSlots) =>
        GetProvider(container).Storage.TryRemoveExact(item, preferredSlot, out changedSlots);

    private static IItemContainerStorageOwner GetProvider(IItemContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return container as IItemContainerStorageOwner ??
            throw new ArgumentException("Item container must provide composed storage.", nameof(container));
    }
}
