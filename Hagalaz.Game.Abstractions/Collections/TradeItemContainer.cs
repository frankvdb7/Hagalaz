using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>
/// Base implementation for containers that explicitly participate in trade mutations.
/// </summary>
public abstract class TradeItemContainer : BaseItemContainer, ITradeItemContainer
{
    /// <summary>
    /// Gets whether the current update notification is restoring a checked trade mutation.
    /// </summary>
    protected bool IsMutationRollbackNotification { get; private set; }

    /// <summary>
    /// Allows a derived trade container to prepare notification state before a checked rollback.
    /// </summary>
    protected virtual void OnMutationRollbackStarting() { }

    /// <summary>
    /// Initializes a trade-capable container with the specified storage behavior and capacity.
    /// </summary>
    protected TradeItemContainer(StorageType type, int capacity)
        : base(type, capacity)
    {
    }

    /// <summary>
    /// Initializes a trade-capable container with the specified storage behavior, items, and capacity.
    /// </summary>
    protected TradeItemContainer(StorageType type, IEnumerable<IItem> items, int capacity)
        : base(type, items, capacity)
    {
    }

    /// <inheritdoc />
    public bool AddRangeForTrade(IEnumerable<IItem?> items)
    {
        var mutation = AddRangeForTradeMutation(items);
        if (mutation.Succeeded)
        {
            return true;
        }

        RollbackOrThrow(mutation);
        return false;
    }

    /// <inheritdoc />
    public bool RemoveForTrade(IItem item, int preferredSlot = -1)
    {
        var mutation = RemoveForTradeMutation(item, preferredSlot);
        if (mutation.Succeeded)
        {
            return true;
        }

        RollbackOrThrow(mutation);
        return false;
    }

    internal TradeItemMutation AddRangeForTradeMutation(IEnumerable<IItem?> items) => AddRangeChecked(items);

    internal TradeItemMutation RemoveForTradeMutation(IItem item, int preferredSlot = -1) =>
        RemoveChecked(item, preferredSlot);

    private TradeItemMutation AddRangeChecked(IEnumerable<IItem?> newItems)
    {
        ArgumentNullException.ThrowIfNull(newItems);

        var changes = new List<SlotChange>();
        var appliedItems = new List<IItem>();
        try
        {
            var items = newItems.ToArray();
            if (!HasSpaceForRange(items))
            {
                return CreateMutation(changes, 0, appliedItems, succeeded: false);
            }

            var slotsToUpdate = new HashSet<int>();
            if (!ApplyTradeAddRange(items, slotsToUpdate, changes, appliedItems, out var appliedCount))
            {
                return CreateMutation(changes, appliedCount, appliedItems, succeeded: false);
            }

            AdvanceRevision();
            var notificationSucceeded = TryNotifyMutation(slotsToUpdate);
            return CreateMutation(
                changes,
                appliedCount,
                appliedItems,
                succeeded: true,
                notificationSucceeded: notificationSucceeded);
        }
        catch (InvalidOperationException)
        {
            return CreateMutation(
                changes,
                changes.Sum(change => change.AppliedCount),
                appliedItems,
                succeeded: false);
        }
    }

    private bool ApplyTradeAddRange(
        IEnumerable<IItem?> newItems,
        HashSet<int> slotsToUpdate,
        List<SlotChange> changes,
        List<IItem> appliedItems,
        out int appliedCount)
    {
        appliedCount = 0;
        using var enumerator = newItems.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (current == null)
            {
                continue;
            }

            for (var i = 0; i < Items.Length; i++)
            {
                var item = Items[i];
                if (item == null || item.Id != current.Id || !item.ItemScript.CanStackItem(item, current, Type == StorageType.AlwaysStack))
                {
                    continue;
                }

                var total = item.Count + (long)current.Count;
                if (total > int.MaxValue)
                {
                    return false;
                }

                CaptureBefore(changes, i);
                item.Count = (int)total;
                UpdateAfter(changes, i);
                slotsToUpdate.Add(i);
                appliedCount += current.Count;
                appliedItems.Add(current.Clone());
                goto end;
            }

            if (Type == StorageType.AlwaysStack || current.ItemDefinition.Stackable || current.ItemDefinition.Noted)
            {
                var slot = GetFreeSlot();
                if (slot == -1)
                {
                    return false;
                }

                CaptureBefore(changes, slot);
                Items[slot] = current;
                UpdateAfter(changes, slot);
                slotsToUpdate.Add(slot);
                appliedCount += current.Count;
                appliedItems.Add(current.Clone());
            }
            else
            {
                if (FreeSlots < current.Count)
                {
                    return false;
                }

                for (var j = 0; j < current.Count; j++)
                {
                    var freeSlot = GetFreeSlot();
                    CaptureBefore(changes, freeSlot);
                    Items[freeSlot] = current.Clone();
                    Items[freeSlot]!.Count = 1;
                    UpdateAfter(changes, freeSlot);
                    slotsToUpdate.Add(freeSlot);
                }

                appliedCount += current.Count;
                appliedItems.Add(current.Clone());
            }

            end:
            {
                continue;
            }
        }

        return true;
    }

    private TradeItemMutation RemoveChecked(IItem item, int preferredSlot)
    {
        ArgumentNullException.ThrowIfNull(item);

        var changes = new List<SlotChange>();
        try
        {
            var slotsToUpdate = new HashSet<int>();
            var removed = ApplyTradeRemove(item, preferredSlot, slotsToUpdate, changes);
            if (removed <= 0)
            {
                return CreateMutation(changes, 0, [], succeeded: true);
            }

            AdvanceRevision();
            var notificationSucceeded = TryNotifyMutation(slotsToUpdate);
            return CreateMutation(
                changes,
                removed,
                [],
                succeeded: true,
                notificationSucceeded: notificationSucceeded);
        }
        catch (InvalidOperationException)
        {
            return CreateMutation(changes, changes.Sum(change => change.AppliedCount), [], succeeded: false);
        }
    }

    private int ApplyTradeRemove(
        IItem item,
        int preferredSlot,
        HashSet<int> slotsToUpdate,
        List<SlotChange> changes)
    {
        var removed = 0;
        if (Type == StorageType.AlwaysStack || item.ItemDefinition.Stackable || item.ItemDefinition.Noted)
        {
            var remaining = item.Count;
            for (var slot = 0; slot < Items.Length; slot++)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var slotItem = Items[slot];
                if (slotItem == null || !slotItem.Equals(item))
                {
                    continue;
                }

                CaptureBefore(changes, slot);
                var removedFromSlot = Math.Min(slotItem.Count, remaining);
                if (slotItem.Count > remaining)
                {
                    slotItem.Count -= remaining;
                }
                else if (CountToResetTo != -1)
                {
                    slotItem.Count = CountToResetTo;
                }
                else
                {
                    Items[slot] = null;
                }

                UpdateAfter(changes, slot);
                slotsToUpdate.Add(slot);
                removed += removedFromSlot;
                remaining -= removedFromSlot;
            }

            return removed;
        }

        var slotIndex = GetSlotByItem(item);
        if (preferredSlot != -1)
        {
            var slotItem = Items[preferredSlot];
            if (slotItem != null && slotItem.Equals(item, true))
            {
                slotIndex = preferredSlot;
            }
        }

        var toRemove = item.Count;
        while (toRemove > 0)
        {
            if (slotIndex == -1 && (slotIndex = GetSlotByItem(item)) == -1)
            {
                break;
            }

            var slotItem = Items[slotIndex];
            if (slotItem == null)
            {
                continue;
            }

            CaptureBefore(changes, slotIndex);
            if (slotItem.Count > toRemove)
            {
                slotItem.Count -= toRemove;
                removed += toRemove;
                UpdateAfter(changes, slotIndex);
                slotsToUpdate.Add(slotIndex);
                break;
            }

            removed += slotItem.Count;
            toRemove -= slotItem.Count;
            if (CountToResetTo != -1)
            {
                slotItem.Count = CountToResetTo;
            }
            else
            {
                Items[slotIndex] = null;
            }

            UpdateAfter(changes, slotIndex);
            slotsToUpdate.Add(slotIndex);
            slotIndex = GetSlotByItem(item);
        }

        return removed;
    }

    private TradeItemMutation CreateMutation(
        IReadOnlyList<SlotChange> changes,
        int appliedCount,
        IReadOnlyList<IItem> appliedItems,
        bool succeeded,
        bool notificationSucceeded = true) =>
        new(appliedCount, succeeded, notificationSucceeded, appliedItems, () => TryRollback(changes));

    private bool TryNotifyMutation(HashSet<int> slotsToUpdate)
    {
        try
        {
            OnUpdate(slotsToUpdate);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void CaptureBefore(List<SlotChange> changes, int slot)
    {
        if (changes.Any(change => change.Slot == slot))
        {
            return;
        }

        var item = Items[slot];
        changes.Add(new SlotChange(slot, item, item?.Count ?? 0));
    }

    private void UpdateAfter(List<SlotChange> changes, int slot)
    {
        var change = changes.FirstOrDefault(candidate => candidate.Slot == slot);
        if (change == null)
        {
            return;
        }

        change.After = Items[slot];
        change.AfterCount = change.After?.Count ?? 0;
    }

    private TradeItemMutation.RollbackOutcome TryRollback(IReadOnlyList<SlotChange> changes)
    {
        if (changes.Count == 0)
        {
            return TradeItemMutation.RollbackOutcome.Restored;
        }

        if (changes.All(change => change.RollbackApplied || Matches(change, before: true)))
        {
            return NotifyRollback(changes);
        }

        if (changes.Any(change => !CanApplyInverse(change)))
        {
            return TradeItemMutation.RollbackOutcome.Failed;
        }

        OnMutationRollbackStarting();
        foreach (var change in changes)
        {
            ApplyInverse(change);
            change.RollbackApplied = true;
        }

        AdvanceRevision();
        return NotifyRollback(changes);
    }

    private TradeItemMutation.RollbackOutcome NotifyRollback(IReadOnlyList<SlotChange> changes)
    {
        try
        {
            IsMutationRollbackNotification = true;
            OnUpdate(changes.Select(change => change.Slot).ToHashSet());
            return TradeItemMutation.RollbackOutcome.Restored;
        }
        catch (InvalidOperationException)
        {
            return TradeItemMutation.RollbackOutcome.Restored;
        }
        finally
        {
            IsMutationRollbackNotification = false;
        }
    }

    private bool CanApplyInverse(SlotChange change)
    {
        if (change.RollbackApplied || Matches(change, before: true))
        {
            return true;
        }

        if (IsAddition(change))
        {
            return true;
        }

        var actual = Items[change.Slot];
        if (ReferenceEquals(change.Before, change.After) && change.After != null)
        {
            if (!ReferenceEquals(actual, change.After))
            {
                return false;
            }

            var delta = (long)change.AfterCount - change.BeforeCount;
            return delta < 0
                ? actual.Count <= int.MaxValue + delta
                : actual.Count >= delta;
        }

        return ReferenceEquals(actual, change.After);
    }

    private void ApplyInverse(SlotChange change)
    {
        if (change.RollbackApplied || Matches(change, before: true))
        {
            return;
        }

        var actual = Items[change.Slot];
        if (IsAddition(change))
        {
            if (!ReferenceEquals(actual, change.After))
            {
                return;
            }

            var addedCount = change.AfterCount - change.BeforeCount;
            if (actual!.Count <= addedCount)
            {
                Items[change.Slot] = null;
            }
            else
            {
                actual.Count -= addedCount;
            }

            return;
        }

        if (ReferenceEquals(change.Before, change.After) && change.After != null)
        {
            var delta = (long)change.AfterCount - change.BeforeCount;
            var restoredCount = actual!.Count - delta;
            actual.Count = (int)restoredCount;
            return;
        }

        if (change.Before == null)
        {
            var remainingCount = actual!.Count - change.AfterCount;
            if (remainingCount <= 0)
            {
                Items[change.Slot] = null;
            }
            else
            {
                actual.Count = remainingCount;
            }

            return;
        }

        Items[change.Slot] = change.Before;
        change.Before.Count = change.BeforeCount;
    }

    private static bool IsAddition(SlotChange change) =>
        change.Before == null && change.After != null ||
        ReferenceEquals(change.Before, change.After) && change.AfterCount > change.BeforeCount;

    private bool Matches(SlotChange change, bool before)
    {
        var expected = before ? change.Before : change.After;
        var expectedCount = before ? change.BeforeCount : change.AfterCount;
        var actual = Items[change.Slot];
        return ReferenceEquals(actual, expected) && (expected == null || actual!.Count == expectedCount);
    }

    private sealed class SlotChange
    {
        public SlotChange(int slot, IItem? before, int beforeCount)
        {
            Slot = slot;
            Before = before;
            BeforeCount = beforeCount;
            After = before;
            AfterCount = beforeCount;
        }

        public int Slot { get; }
        public IItem? Before { get; }
        public int BeforeCount { get; }
        public IItem? After { get; set; }
        public int AfterCount { get; set; }
        public bool RollbackApplied { get; set; }
        public int AppliedCount => Math.Abs(AfterCount - BeforeCount);
    }

    private static void RollbackOrThrow(TradeItemMutation mutation)
    {
        if (!mutation.TryRollback())
        {
            throw new InvalidOperationException("Trade container operation could not be restored.");
        }
    }
}
