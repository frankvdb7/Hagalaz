using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Coordinates one synchronous, rollback-capable mutation across item-container boundaries.</summary>
public sealed class ItemContainerTransaction
{
    private readonly List<ItemContainerMutationBoundary> _boundaries = [];
    private readonly Dictionary<ItemContainerMutationBoundary, HashSet<int>?> _changed =
        new(ReferenceEqualityComparer.Instance);
    private bool _executed;
    private bool _executing;

    public ItemContainerTransaction(params ItemContainerMutationBoundary[] boundaries)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
        foreach (var boundary in boundaries) Include(boundary);
    }

    /// <summary>Adds a domain-owned boundary before this transaction executes.</summary>
    public void Include(ItemContainerMutationBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        if (_executed) throw new InvalidOperationException("A transaction cannot be changed after execution starts.");
        if (!_boundaries.Any(existing => ReferenceEquals(existing.Storage, boundary.Storage)))
        {
            _boundaries.Add(boundary);
        }
    }

    /// <summary>Executes staged operations under ordered locks and publishes committed changes after unlocking.</summary>
    public bool TryExecute(Func<ItemContainerTransaction, bool> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (_executed) throw new InvalidOperationException("A transaction can execute only once.");
        if (_boundaries.Count == 0) throw new InvalidOperationException("A transaction requires at least one boundary.");

        _executed = true;
        var snapshots = new List<StorageSnapshot>();
        var succeeded = false;
        try
        {
            _boundaries[0].WithLocks(_boundaries, () =>
            {
                snapshots.AddRange(_boundaries.Select(boundary => boundary.Storage)
                    .Distinct()
                    .Select(storage => Capture(storage, _boundaries.First(boundary => ReferenceEquals(boundary.Storage, storage)))));

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
        }
        catch
        {
            throw;
        }

        if (succeeded)
        {
            foreach (var (boundary, slots) in _changed)
            {
                boundary.PublishChanges(slots);
            }
        }

        return succeeded;
    }

    public bool TryAddRange(ItemContainerMutationBoundary boundary, IEnumerable<IItem?> items) =>
        TryAddRange(boundary, items, out _);

    public bool TryAddRange(ItemContainerMutationBoundary boundary, IEnumerable<IItem?> items,
        out HashSet<int> changedSlots)
    {
        EnsureParticipant(boundary);
        if (!boundary.Storage.TryAddRange(items, out changedSlots)) return false;
        RecordChangedSlots(boundary, changedSlots);
        return true;
    }

    public bool TryRemoveExact(ItemContainerMutationBoundary boundary, IItem item, int preferredSlot = -1) =>
        TryRemoveExact(boundary, item, preferredSlot, out _);

    public bool TryRemoveExact(ItemContainerMutationBoundary boundary, IItem item, int preferredSlot,
        out HashSet<int> changedSlots)
    {
        EnsureParticipant(boundary);
        if (!boundary.Storage.TryRemoveExact(item, preferredSlot, out changedSlots)) return false;
        RecordChangedSlots(boundary, changedSlots);
        return true;
    }

    public bool TryTransfer(
        ItemContainerMutationBoundary source,
        ItemContainerMutationBoundary destination,
        IItem item,
        int count,
        int preferredSourceSlot = -1,
        int destinationSlot = -1,
        IItem? destinationItem = null)
    {
        EnsureParticipant(source);
        EnsureParticipant(destination);
        if (!source.TryTransferToStorage(destination, item, count, preferredSourceSlot, destinationSlot,
                destinationItem, out var sourceSlots, out var destinationSlots))
        {
            return false;
        }

        RecordChangedSlots(source, sourceSlots);
        RecordChangedSlots(destination, destinationSlots);
        return true;
    }

    public bool Clear(ItemContainerMutationBoundary boundary)
    {
        EnsureParticipant(boundary);
        if (!boundary.Storage.Clear()) return false;
        _changed[boundary] = null;
        return true;
    }

    public void RecordChangedSlots(ItemContainerMutationBoundary boundary, IEnumerable<int> slots)
    {
        EnsureParticipant(boundary);
        if (_changed.TryGetValue(boundary, out var changedSlots))
        {
            if (changedSlots != null) changedSlots.UnionWith(slots);
        }
        else
        {
            _changed.Add(boundary, new HashSet<int>(slots));
        }
    }

    public void RecordFullChange(ItemContainerMutationBoundary boundary)
    {
        EnsureParticipant(boundary);
        _changed[boundary] = null;
    }

    private void EnsureParticipant(ItemContainerMutationBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        if (!_executing) throw new InvalidOperationException("Storage changes can be staged only while executing the transaction.");
        if (!_boundaries.Any(existing => ReferenceEquals(existing.Storage, boundary.Storage)))
        {
            throw new ArgumentException("The boundary is not part of this transaction.", nameof(boundary));
        }
    }

    private static StorageSnapshot Capture(ItemContainerStorage storage, ItemContainerMutationBoundary boundary)
    {
        var items = storage.ToArray();
        var counts = items.Select(item => item?.Count ?? 0).ToArray();
        return new StorageSnapshot(storage, boundary, items, counts);
    }

    private void RestoreChanged(IEnumerable<StorageSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots.Where(snapshot => _changed.ContainsKey(snapshot.Boundary)))
        {
            for (var slot = 0; slot < snapshot.Items.Length; slot++)
            {
                if (snapshot.Items[slot] != null) snapshot.Items[slot]!.Count = snapshot.Counts[slot];
            }

            snapshot.Storage.ReplaceState(snapshot.Items);
        }
        _changed.Clear();
    }

    private sealed record StorageSnapshot(ItemContainerStorage Storage, ItemContainerMutationBoundary Boundary,
        IItem?[] Items, int[] Counts);
}
