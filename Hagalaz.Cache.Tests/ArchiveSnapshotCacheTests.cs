using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Metrics;
using Hagalaz.Cache.Models;
using Xunit;

namespace Hagalaz.Cache.Tests;

[Collection(CacheApiTelemetryCollection.Name)]
public sealed class ArchiveSnapshotCacheTests
{
    [Fact]
    public void ReadMember_RetainsExactBudgetAndReportsAccessAndByteAccounting()
    {
        var snapshot = CreateSnapshot([1, 2, 3]);
        var cache = new ArchiveSnapshotCache(snapshot.RetentionWeight);
        var retainedBytes = new ConcurrentQueue<long>();
        var accesses = new ConcurrentQueue<string>();
        using var listener = CreateListener((instrument, value, tags) =>
        {
            if (instrument.Name == "hagalaz.cache.archive.snapshot.retained.bytes")
            {
                retainedBytes.Enqueue(value);
            }
            else if (instrument.Name == "hagalaz.cache.archive.snapshot.access")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "result")
                    {
                        accesses.Enqueue((string)tag.Value!);
                    }
                }
            }
        });
        var loads = 0;

        using (cache.ReadMember(new ArchiveKey(0, 0), 0, () =>
               {
                   Interlocked.Increment(ref loads);
                   return snapshot;
               }))
        {
        }

        using (cache.ReadMember(new ArchiveKey(0, 0), 0, () =>
               {
                   Interlocked.Increment(ref loads);
                   return snapshot;
               }))
        {
        }

        cache.Invalidate(new ArchiveKey(0, 0));

        Assert.Equal(1, loads);
        Assert.Equal(new[] { "miss", "hit" }, accesses.ToArray());
        Assert.Equal(new[] { snapshot.RetentionWeight, -snapshot.RetentionWeight }, retainedBytes.ToArray());
    }

    [Fact]
    public void InvalidateIndex_RemovesOnlySnapshotsForThatIndex()
    {
        var cache = new ArchiveSnapshotCache(1024 * 1024);
        var loads = new Dictionary<ArchiveKey, int>();
        var keys = new[] { new ArchiveKey(16, 1), new ArchiveKey(16, 2), new ArchiveKey(17, 1) };

        foreach (var key in keys)
        {
            using var stream = cache.ReadMember(key, 0, () => Load(key));
        }

        cache.InvalidateIndex(16);

        foreach (var key in keys)
        {
            using var stream = cache.ReadMember(key, 0, () => Load(key));
        }

        Assert.Equal(2, loads[new ArchiveKey(16, 1)]);
        Assert.Equal(2, loads[new ArchiveKey(16, 2)]);
        Assert.Equal(1, loads[new ArchiveKey(17, 1)]);

        ArchiveSnapshot Load(ArchiveKey key)
        {
            loads.TryGetValue(key, out var count);
            loads[key] = count + 1;
            return CreateSnapshot([(byte)key.ArchiveId]);
        }
    }

    [Fact]
    public void ReadMember_EvictsLeastRecentlyUsedEntryAndRetainsFittingSnapshots()
    {
        var firstSnapshot = CreateSnapshot([1]);
        var secondSnapshot = CreateSnapshot([2]);
        var thirdSnapshot = CreateSnapshot([3]);
        var cache = new ArchiveSnapshotCache(firstSnapshot.RetentionWeight * 2);
        var loads = new int[3];

        using (cache.ReadMember(new ArchiveKey(0, 1), 0, Load(0, firstSnapshot))) { }
        using (cache.ReadMember(new ArchiveKey(0, 2), 0, Load(1, secondSnapshot))) { }
        using (cache.ReadMember(new ArchiveKey(0, 1), 0, Load(0, firstSnapshot))) { }
        using (cache.ReadMember(new ArchiveKey(0, 3), 0, Load(2, thirdSnapshot))) { }
        using (cache.ReadMember(new ArchiveKey(0, 1), 0, Load(0, firstSnapshot))) { }
        using (cache.ReadMember(new ArchiveKey(0, 3), 0, Load(2, thirdSnapshot))) { }

        Assert.Equal(new[] { 1, 1, 1 }, loads);

        using (cache.ReadMember(new ArchiveKey(0, 2), 0, Load(1, secondSnapshot))) { }

        Assert.Equal(new[] { 1, 2, 1 }, loads);

        Func<ArchiveSnapshot> Load(int index, ArchiveSnapshot snapshot) => () =>
        {
            Interlocked.Increment(ref loads[index]);
            return snapshot;
        };
    }

    [Fact]
    public void ReadMember_DoesNotRetainOversizedOrZeroCapacitySnapshots()
    {
        var snapshot = CreateSnapshot([1, 2]);
        Assert.True(snapshot.RetentionWeight > 0);

        foreach (var capacity in new[] { 0L, snapshot.RetentionWeight - 1 })
        {
            var cache = new ArchiveSnapshotCache(capacity);
            var loads = 0;
            using (cache.ReadMember(new ArchiveKey(0, 0), 0, Load)) { }
            using (cache.ReadMember(new ArchiveKey(0, 0), 0, Load)) { }
            Assert.Equal(2, loads);

            ArchiveSnapshot Load()
            {
                loads++;
                return snapshot;
            }
        }
    }

    [Fact]
    public void Constructor_RejectsNegativeCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArchiveSnapshotCache(-1));
    }

    [Fact]
    public async Task ReadMember_SharesAnAttachedColdLoadAndPublishesOnlyOnce()
    {
        var snapshot = CreateSnapshot([1, 2, 3]);
        var cache = new ArchiveSnapshotCache(snapshot.RetentionWeight * 2);
        using var loaderStarted = new ManualResetEventSlim();
        using var releaseLoader = new ManualResetEventSlim();
        using var joinSignal = new FlightJoinSignal();
        var loads = 0;
        var first = Task.Run(() => cache.ReadMember(new ArchiveKey(0, 1), 0, Load));
        Assert.True(loaderStarted.Wait(TimeSpan.FromSeconds(10)));

        var second = Task.Run(() => cache.ReadMember(new ArchiveKey(0, 1), 0, Load));
        try
        {
            Assert.True(joinSignal.Joined.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseLoader.Set();
        }

        using var firstStream = await first;
        using var secondStream = await second;
        Assert.Equal(new byte[] { 1, 2, 3 }, firstStream.ToArray());
        Assert.Equal(new byte[] { 1, 2, 3 }, secondStream.ToArray());
        Assert.Equal(1, loads);

        using var retainedStream = cache.ReadMember(new ArchiveKey(0, 1), 0, Load);
        Assert.Equal(1, loads);

        ArchiveSnapshot Load()
        {
            Interlocked.Increment(ref loads);
            loaderStarted.Set();
            if (!releaseLoader.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the gated snapshot load.");
            }

            return snapshot;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReadMember_SharesUnretainedFlightOnlyWhileCallersAreAttached(long capacityOffset)
    {
        var snapshot = CreateSnapshot([7, 8], []);
        var capacity = capacityOffset == 0 ? 0 : snapshot.RetentionWeight - 1;
        var cache = new ArchiveSnapshotCache(capacity);
        using var loaderStarted = new ManualResetEventSlim();
        using var releaseLoader = new ManualResetEventSlim();
        using var joinSignal = new FlightJoinSignal();
        var loads = 0;

        var first = Task.Run(() => cache.ReadMember(new ArchiveKey(3, 4), 0, Load));
        Assert.True(loaderStarted.Wait(TimeSpan.FromSeconds(10)));
        var second = Task.Run(() => cache.ReadMember(new ArchiveKey(3, 4), 1, Load));
        try
        {
            Assert.True(joinSignal.Joined.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseLoader.Set();
        }

        using var firstStream = await first;
        using var secondStream = await second;
        Assert.Equal(new byte[] { 7, 8 }, firstStream.ToArray());
        Assert.Equal(0, secondStream.Length);
        Assert.Equal(1, loads);

        using var laterStream = cache.ReadMember(new ArchiveKey(3, 4), 0, Load);
        Assert.Equal(2, loads);

        ArchiveSnapshot Load()
        {
            Interlocked.Increment(ref loads);
            loaderStarted.Set();
            if (!releaseLoader.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the gated snapshot load.");
            }

            return snapshot;
        }
    }

    [Fact]
    public async Task ReadMember_LoadsDifferentArchivesIndependently()
    {
        var cache = new ArchiveSnapshotCache(0);
        using var bothLoadersStarted = new CountdownEvent(2);
        using var releaseLoaders = new ManualResetEventSlim();
        var firstSnapshot = CreateSnapshot([1]);
        var secondSnapshot = CreateSnapshot([2]);

        var first = Task.Run(() => cache.ReadMember(new ArchiveKey(0, 1), 0, Load(firstSnapshot)));
        var second = Task.Run(() => cache.ReadMember(new ArchiveKey(0, 2), 0, Load(secondSnapshot)));
        try
        {
            Assert.True(bothLoadersStarted.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseLoaders.Set();
        }

        using var firstStream = await first;
        using var secondStream = await second;
        Assert.Equal(new byte[] { 1 }, firstStream.ToArray());
        Assert.Equal(new byte[] { 2 }, secondStream.ToArray());

        Func<ArchiveSnapshot> Load(ArchiveSnapshot snapshot) => () =>
        {
            bothLoadersStarted.Signal();
            if (!releaseLoaders.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the gated snapshot loads.");
            }

            return snapshot;
        };
    }

    [Fact]
    public void ReadMember_FailedLoadDoesNotPoisonRetryOrRetainFailure()
    {
        var cache = new ArchiveSnapshotCache(1024);
        var failure = new InvalidDataException("invalid archive");
        var actual = Assert.Throws<InvalidDataException>(() => cache.ReadMember(
            new ArchiveKey(0, 0), 0, () => throw failure));

        Assert.Same(failure, actual);

        var loads = 0;
        var snapshot = CreateSnapshot([9]);
        using var stream = cache.ReadMember(new ArchiveKey(0, 0), 0, () =>
        {
            loads++;
            return snapshot;
        });

        Assert.Equal(new byte[] { 9 }, stream.ToArray());
        Assert.Equal(1, loads);
    }

    private static ArchiveSnapshot CreateSnapshot(params byte[][] members)
    {
        var archiveBytes = ArchiveTestData.EncodeArchive(members);
        using var memberData = new MemoryStream(archiveBytes);
        using var container = new Container(memberData);
        using var archive = new ArchiveDecoder().Decode(container, members.Length);
        return ArchiveSnapshot.Create(archive);
    }

    private static MeterListener CreateListener(Action<Instrument, long, KeyValuePair<string, object?>[]> measurement)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == CacheMetrics.MeterName)
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => measurement(instrument, value, tags.ToArray()));
        listener.Start();
        return listener;
    }

    private sealed class FlightJoinSignal : IDisposable
    {
        private readonly MeterListener _listener = new();

        public FlightJoinSignal()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == CacheMetrics.MeterName && instrument.Name == "hagalaz.cache.archive.snapshot.flight.join")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            {
                if (instrument.Name == "hagalaz.cache.archive.snapshot.flight.join")
                {
                    Joined.Set();
                }
            });
            _listener.Start();
        }

        public ManualResetEventSlim Joined { get; } = new();

        public void Dispose()
        {
            _listener.Dispose();
            Joined.Dispose();
        }
    }
}
