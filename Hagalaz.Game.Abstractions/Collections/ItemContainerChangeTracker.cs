using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Tracks committed item-container changes and consumes them in batches.</summary>
public sealed class ItemContainerChangeTracker
{
    private readonly ItemContainerSynchronization _synchronization;
    private ChangeSet _committed;
    private ChangeSet _pending;

    internal ItemContainerChangeTracker(ItemContainerSynchronization synchronization) => _synchronization = synchronization;

    /// <summary>Gets whether committed, unconsumed changes exist.</summary>
    public bool HasChanges => _synchronization.HasTrackedChanges;

    /// <summary>Gets the synchronized mutation revision of the underlying item storage.</summary>
    /// <remarks>A rolled-back transaction restores its previous revision. The owning thread may observe in-progress mutations.</remarks>
    public int Revision => _synchronization.MutationRevision;

    /// <summary>
    /// Retrieves and consumes committed changes. Returns <see langword="null"/> when the entire container changed,
    /// an empty set when there are no changes, or the changed slot indexes otherwise.
    /// </summary>
    public HashSet<int>? GetSlots() => _synchronization.ConsumeTrackedChanges();

    internal bool HasCommittedChanges => _committed.HasChanges;

    internal void RecordCommitted(HashSet<int>? slots) => Merge(ref _committed, slots);

    internal void RecordPending(HashSet<int>? slots) => Merge(ref _pending, slots);

    internal void PromotePending()
    {
        if (!_pending.HasChanges) return;
        Merge(ref _committed, _pending.Slots);
        _pending = default;
    }

    internal void DiscardPending() => _pending = default;

    internal void DiscardCommitted() => _committed = default;

    internal bool TryConsumeCommitted(out HashSet<int>? slots)
    {
        if (!_committed.HasChanges)
        {
            slots = [];
            return false;
        }

        slots = _committed.Slots;
        _committed = default;
        return true;
    }

    private static void Merge(ref ChangeSet target, HashSet<int>? slots)
    {
        if (slots is { Count: 0 }) return;

        if (!target.HasChanges)
        {
            target = new ChangeSet(true, slots == null ? null : new HashSet<int>(slots));
            return;
        }

        if (target.Slots == null) return;
        if (slots == null)
        {
            target.Slots = null;
            return;
        }

        target.Slots.UnionWith(slots);
    }

    private struct ChangeSet(bool hasChanges, HashSet<int>? slots)
    {
        internal bool HasChanges = hasChanges;
        internal HashSet<int>? Slots = slots;
    }
}
