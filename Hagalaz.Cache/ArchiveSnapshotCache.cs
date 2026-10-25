using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Hagalaz.Cache.Metrics;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache;

internal readonly record struct ArchiveKey(int IndexId, int ArchiveId);

internal sealed class ArchiveSnapshotCache
{
    private readonly object _sync = new();
    private readonly long _capacityBytes;
    private readonly Dictionary<ArchiveKey, CacheEntry> _entries = [];
    private readonly LinkedList<ArchiveKey> _lru = [];
    private readonly Dictionary<ArchiveKey, LoadFlight> _flights = [];
    private long _retainedBytes;

    public ArchiveSnapshotCache(long capacityBytes)
    {
        if (capacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityBytes), "Snapshot cache capacity cannot be negative.");
        }

        _capacityBytes = capacityBytes;
    }

    public MemoryStream ReadMember(ArchiveKey key, int physicalMemberIndex, Func<ArchiveSnapshot> loadSnapshot)
    {
        ArchiveSnapshot? retainedSnapshot = null;
        LoadFlight? flight = null;
        var joinedFlight = false;

        lock (_sync)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                _lru.Remove(entry.Node);
                _lru.AddFirst(entry.Node);
                retainedSnapshot = entry.Snapshot;
            }
            else if (_flights.TryGetValue(key, out flight))
            {
                flight.ActiveParticipants++;
                joinedFlight = true;
            }
            else
            {
                flight = new LoadFlight(loadSnapshot);
                _flights.Add(key, flight);
            }
        }

        if (retainedSnapshot is not null)
        {
            CacheMetrics.RecordArchiveSnapshotAccess(hit: true);
            return retainedSnapshot.OpenMember(physicalMemberIndex);
        }

        try
        {
            CacheMetrics.RecordArchiveSnapshotAccess(hit: false);
            if (joinedFlight)
            {
                CacheMetrics.RecordArchiveSnapshotFlightJoin();
            }

            ArchiveSnapshot snapshot;
            try
            {
                snapshot = flight!.Snapshot.Value;
            }
            catch
            {
                RemoveFailedFlight(key, flight!);
                throw;
            }

            CompleteSuccessfulFlight(key, flight!, snapshot);
            return snapshot.OpenMember(physicalMemberIndex);
        }
        finally
        {
            ReleaseFlight(key, flight!);
        }
    }

    public void Invalidate(ArchiveKey key)
    {
        long retainedByteDelta = 0;

        lock (_sync)
        {
            if (_entries.Remove(key, out var entry))
            {
                _lru.Remove(entry.Node);
                _retainedBytes -= entry.Snapshot.RetentionWeight;
                retainedByteDelta = -entry.Snapshot.RetentionWeight;
            }
        }

        CacheMetrics.RecordArchiveSnapshotRetainedBytes(retainedByteDelta);
    }

    public void InvalidateIndex(int indexId)
    {
        long retainedByteDelta = 0;

        lock (_sync)
        {
            var node = _lru.First;
            while (node is not null)
            {
                var next = node.Next;
                if (node.Value.IndexId == indexId)
                {
                    var entry = _entries[node.Value];
                    _entries.Remove(node.Value);
                    _lru.Remove(node);
                    _retainedBytes -= entry.Snapshot.RetentionWeight;
                    retainedByteDelta = checked(retainedByteDelta - entry.Snapshot.RetentionWeight);
                }

                node = next;
            }
        }

        CacheMetrics.RecordArchiveSnapshotRetainedBytes(retainedByteDelta);
    }

    public void Clear()
    {
        long retainedBytes;

        lock (_sync)
        {
            retainedBytes = _retainedBytes;
            _entries.Clear();
            _lru.Clear();
            _retainedBytes = 0;
        }

        if (retainedBytes > 0)
        {
            CacheMetrics.RecordArchiveSnapshotRetainedBytes(-retainedBytes);
        }
    }

    private void CompleteSuccessfulFlight(ArchiveKey key, LoadFlight flight, ArchiveSnapshot snapshot)
    {
        long retainedByteDelta = 0;
        long evictions = 0;

        lock (_sync)
        {
            if (flight.CompletionHandled)
            {
                return;
            }

            flight.CompletionHandled = true;
            var weight = snapshot.RetentionWeight;
            if (_capacityBytes == 0 || weight > _capacityBytes)
            {
                return;
            }

            long evictedBytes = 0;
            while (_retainedBytes > _capacityBytes - weight)
            {
                var node = _lru.Last ?? throw new InvalidOperationException("Snapshot LRU is inconsistent with retained entries.");
                var evictedKey = node.Value;
                var evictedEntry = _entries[evictedKey];
                _lru.RemoveLast();
                _entries.Remove(evictedKey);
                _retainedBytes -= evictedEntry.Snapshot.RetentionWeight;
                evictedBytes = checked(evictedBytes + evictedEntry.Snapshot.RetentionWeight);
                evictions++;
            }

            var newNode = _lru.AddFirst(key);
            _entries.Add(key, new CacheEntry(snapshot, newNode));
            _retainedBytes = checked(_retainedBytes + weight);
            retainedByteDelta = checked(weight - evictedBytes);
            RemoveFlightIfCurrent(key, flight);
        }

        if (evictions > 0)
        {
            CacheMetrics.RecordArchiveSnapshotEvictions(evictions);
        }

        CacheMetrics.RecordArchiveSnapshotRetainedBytes(retainedByteDelta);
    }

    private void RemoveFailedFlight(ArchiveKey key, LoadFlight flight)
    {
        lock (_sync)
        {
            flight.CompletionHandled = true;
            RemoveFlightIfCurrent(key, flight);
        }
    }

    private void ReleaseFlight(ArchiveKey key, LoadFlight flight)
    {
        lock (_sync)
        {
            flight.ActiveParticipants--;
            if (flight.ActiveParticipants == 0)
            {
                RemoveFlightIfCurrent(key, flight);
            }
        }
    }

    private void RemoveFlightIfCurrent(ArchiveKey key, LoadFlight flight)
    {
        if (_flights.TryGetValue(key, out var current) && ReferenceEquals(current, flight))
        {
            _flights.Remove(key);
        }
    }

    private sealed class CacheEntry(ArchiveSnapshot snapshot, LinkedListNode<ArchiveKey> node)
    {
        public ArchiveSnapshot Snapshot { get; } = snapshot;
        public LinkedListNode<ArchiveKey> Node { get; } = node;
    }

    private sealed class LoadFlight
    {
        public LoadFlight(Func<ArchiveSnapshot> loadSnapshot)
        {
            Snapshot = new Lazy<ArchiveSnapshot>(loadSnapshot, LazyThreadSafetyMode.ExecutionAndPublication);
            ActiveParticipants = 1;
        }

        public Lazy<ArchiveSnapshot> Snapshot { get; }
        public int ActiveParticipants { get; set; }
        public bool CompletionHandled { get; set; }
    }
}
