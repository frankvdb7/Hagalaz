using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates one synchronous, rollback-capable mutation across item-container boundaries.</summary>
public sealed class ItemContainerTransaction : IItemContainerTransaction
{
    private readonly List<Participant> _participants = [];
    private readonly Dictionary<Participant, HashSet<int>?> _changed = new(ReferenceEqualityComparer.Instance);
    private readonly List<Action> _beforePublish = [];
    private readonly List<Action> _committed = [];
    private bool _executed;
    private bool _executing;

    public ItemContainerTransaction(params IItemContainerMutationBoundary[] boundaries)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
        foreach (var boundary in boundaries) Include(boundary);
    }

    public void Include(IItemContainerMutationBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        if (_executed) throw new InvalidOperationException("A transaction cannot be changed after execution starts.");
        boundary.Enlist(this);
    }

    internal void RegisterParticipant(IItemContainerMutationBoundary boundary, ItemContainerStorage storage,
        Action<HashSet<int>?>? publishChanges)
    {
        if (!_participants.Any(existing => ReferenceEquals(existing.Storage, storage)))
        {
            _participants.Add(new Participant(boundary, storage, publishChanges));
        }
    }

    public bool TryExecute(Func<IItemContainerTransaction, bool> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (_executed) throw new InvalidOperationException("A transaction can execute only once.");
        if (_participants.Count == 0) throw new InvalidOperationException("A transaction requires at least one boundary.");

        _executed = true;
        var snapshots = new List<StorageSnapshot>();
        var succeeded = false;
        WithLocks(_participants, () =>
        {
            snapshots.AddRange(_participants.Select(participant => Capture(participant)));
            _executing = true;
            try
            {
                succeeded = operation(this);
                if (!succeeded) RestoreChanged(snapshots);
            }
            catch
            {
                RestoreChanged(snapshots);
                throw;
            }
            finally
            {
                _executing = false;
            }
        });

        if (!succeeded)
        {
            _committed.Clear();
            return false;
        }

        foreach (var callback in _beforePublish) callback();
        foreach (var (participant, slots) in _changed)
        {
            participant.PublishChanges?.Invoke(slots);
        }
        foreach (var callback in _committed) callback();
        return true;
    }

    public bool TryAddRange(IItemContainerMutationBoundary boundary, IEnumerable<IItem?> items) =>
        TryAddRange(boundary, items, out _);

    public bool TryAddRange(IItemContainerMutationBoundary boundary, IEnumerable<IItem?> items,
        out HashSet<int> changedSlots)
    {
        var participant = EnsureParticipant(boundary);
        if (!participant.Storage.TryAddRange(items, out changedSlots)) return false;
        RecordChangedSlots(boundary, changedSlots);
        return true;
    }

    public bool TryRemoveExact(IItemContainerMutationBoundary boundary, IItem item, int preferredSlot = -1) =>
        TryRemoveExact(boundary, item, preferredSlot, out _);

    public bool TryRemoveExact(IItemContainerMutationBoundary boundary, IItem item, int preferredSlot,
        out HashSet<int> changedSlots)
    {
        var participant = EnsureParticipant(boundary);
        if (!participant.Storage.TryRemoveExact(item, preferredSlot, out changedSlots)) return false;
        RecordChangedSlots(boundary, changedSlots);
        return true;
    }

    public bool TryTransfer(IItemContainerMutationBoundary source, IItemContainerMutationBoundary destination,
        IItem item, int count, int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
    {
        var sourceParticipant = EnsureParticipant(source);
        var destinationParticipant = EnsureParticipant(destination);
        if (!ItemContainerStorage.TryTransfer(sourceParticipant.Storage, destinationParticipant.Storage, item, count,
                preferredSourceSlot, destinationSlot, destinationItem, out var sourceSlots, out var destinationSlots))
        {
            return false;
        }

        RecordChangedSlots(source, sourceSlots);
        RecordChangedSlots(destination, destinationSlots);
        return true;
    }

    public void TransferAll(IItemContainerMutationBoundary source, IItemContainerMutationBoundary destination)
    {
        var sourceParticipant = EnsureParticipant(source);
        EnsureParticipant(destination);
        for (var slot = 0; slot < sourceParticipant.Storage.Capacity; slot++)
        {
            var item = sourceParticipant.Storage[slot];
            if (item is { Count: > 0 }) TryTransfer(source, destination, item, item.Count, slot);
        }
    }

    public bool Clear(IItemContainerMutationBoundary boundary)
    {
        var participant = EnsureParticipant(boundary);
        if (!participant.Storage.Clear()) return false;
        _changed[participant] = null;
        return true;
    }

    public void RecordChangedSlots(IItemContainerMutationBoundary boundary, IEnumerable<int> slots)
    {
        var participant = EnsureParticipant(boundary);
        if (_changed.TryGetValue(participant, out var changedSlots))
        {
            changedSlots?.UnionWith(slots);
        }
        else
        {
            _changed.Add(participant, new HashSet<int>(slots));
        }
    }

    public void RecordFullChange(IItemContainerMutationBoundary boundary) => _changed[EnsureParticipant(boundary)] = null;

    public void OnCommitted(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_executing) throw new InvalidOperationException("Callbacks can be registered only while executing the transaction.");
        _committed.Add(action);
    }

    /// <summary>Registers a domain callback that must run after unlocking and before participant publication.</summary>
    public void OnCommittedBeforePublish(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_executing) throw new InvalidOperationException("Callbacks can be registered only while executing the transaction.");
        _beforePublish.Add(action);
    }

    private Participant EnsureParticipant(IItemContainerMutationBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        if (!_executing) throw new InvalidOperationException("Storage changes can be staged only while executing the transaction.");
        return _participants.FirstOrDefault(participant => ReferenceEquals(participant.Boundary, boundary))
               ?? throw new ArgumentException("The boundary is not part of this transaction.", nameof(boundary));
    }

    private static StorageSnapshot Capture(Participant participant)
    {
        var items = participant.Storage.ToArray();
        var counts = items.Select(item => item?.Count ?? 0).ToArray();
        return new StorageSnapshot(participant, items, counts);
    }

    private void RestoreChanged(IEnumerable<StorageSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots.Where(snapshot => _changed.ContainsKey(snapshot.Participant)))
        {
            for (var slot = 0; slot < snapshot.Items.Length; slot++)
            {
                if (snapshot.Items[slot] != null) snapshot.Items[slot]!.Count = snapshot.Counts[slot];
            }
            snapshot.Participant.Storage.ReplaceState(snapshot.Items);
        }
        _changed.Clear();
        _beforePublish.Clear();
        _committed.Clear();
    }

    private static void WithLocks(IEnumerable<Participant> participants, Action operation)
    {
        var storages = participants.Select(participant => participant.Storage)
            .Distinct().OrderBy(storage => storage.MutationOrder).ToArray();
        WithLockAt(0);
        void WithLockAt(int index)
        {
            if (index == storages.Length) { operation(); return; }
            lock (storages[index].MutationLock) WithLockAt(index + 1);
        }
    }

    private sealed record Participant(IItemContainerMutationBoundary Boundary, ItemContainerStorage Storage,
        Action<HashSet<int>?>? PublishChanges);
    private sealed record StorageSnapshot(Participant Participant, IItem?[] Items, int[] Counts);
}
