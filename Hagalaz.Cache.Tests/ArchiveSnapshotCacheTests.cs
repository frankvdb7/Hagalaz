using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
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
        var key = new ArchiveKey(0, 0);
        var snapshot = CreateSnapshot([1, 2, 3]);
        using var fixture = new SnapshotCacheFixture();
        fixture.SetArchive(key, [1, 2, 3]);
        var cache = fixture.CreateCache(snapshot.RetentionWeight);
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

        using (cache.ReadMember(key, 0, 1)) { }
        using (cache.ReadMember(key, 0, 1)) { }
        cache.Invalidate(key);

        Assert.Equal(1, fixture.GetReadCount(key));
        Assert.Equal(new[] { "miss", "hit" }, accesses.ToArray());
        Assert.Equal(new[] { snapshot.RetentionWeight, -snapshot.RetentionWeight }, retainedBytes.ToArray());
    }

    [Fact]
    public void ReadMember_RecordsContainerDecodeOnlyForColdArchiveLoad()
    {
        var key = new ArchiveKey(0, 0);
        using var fixture = new SnapshotCacheFixture();
        fixture.SetArchive(key, [1, 2, 3]);
        var cache = fixture.CreateCache(1024);
        var measurements = new ConcurrentQueue<string>();
        using var listener = CreateDurationListener(instrument => measurements.Enqueue(instrument.Name));

        using (cache.ReadMember(key, 0, 1)) { }
        using (cache.ReadMember(key, 0, 1)) { }

        Assert.Equal(1, measurements.Count(name => name == "hagalaz.cache.container.decode.duration"));
        Assert.Equal(1, measurements.Count(name => name == "hagalaz.cache.archive.load.duration"));
    }

    [Fact]
    public void InvalidateIndex_RemovesOnlySnapshotsForThatIndex()
    {
        using var fixture = new SnapshotCacheFixture();
        var cache = fixture.CreateCache(1024 * 1024);
        var keys = new[] { new ArchiveKey(16, 1), new ArchiveKey(16, 2), new ArchiveKey(17, 1) };

        foreach (var key in keys)
        {
            fixture.SetArchive(key, [(byte)key.ArchiveId]);
            using var stream = cache.ReadMember(key, 0, 1);
        }

        cache.InvalidateIndex(16);

        foreach (var key in keys)
        {
            using var stream = cache.ReadMember(key, 0, 1);
        }

        Assert.Equal(2, fixture.GetReadCount(new ArchiveKey(16, 1)));
        Assert.Equal(2, fixture.GetReadCount(new ArchiveKey(16, 2)));
        Assert.Equal(1, fixture.GetReadCount(new ArchiveKey(17, 1)));
    }

    [Fact]
    public void ReadMember_EvictsLeastRecentlyUsedEntryAndRetainsFittingSnapshots()
    {
        var firstSnapshot = CreateSnapshot([1]);
        var cacheCapacity = firstSnapshot.RetentionWeight * 2;
        using var fixture = new SnapshotCacheFixture();
        var cache = fixture.CreateCache(cacheCapacity);
        var keys = new[] { new ArchiveKey(0, 1), new ArchiveKey(0, 2), new ArchiveKey(0, 3) };
        fixture.SetArchive(keys[0], [1]);
        fixture.SetArchive(keys[1], [2]);
        fixture.SetArchive(keys[2], [3]);

        using (cache.ReadMember(keys[0], 0, 1)) { }
        using (cache.ReadMember(keys[1], 0, 1)) { }
        using (cache.ReadMember(keys[0], 0, 1)) { }
        using (cache.ReadMember(keys[2], 0, 1)) { }
        using (cache.ReadMember(keys[0], 0, 1)) { }
        using (cache.ReadMember(keys[2], 0, 1)) { }

        Assert.Equal(1, fixture.GetReadCount(keys[0]));
        Assert.Equal(1, fixture.GetReadCount(keys[1]));
        Assert.Equal(1, fixture.GetReadCount(keys[2]));

        using (cache.ReadMember(keys[1], 0, 1)) { }

        Assert.Equal(2, fixture.GetReadCount(keys[1]));
    }

    [Fact]
    public void ReadMember_DoesNotRetainOversizedOrZeroCapacitySnapshots()
    {
        var snapshot = CreateSnapshot([1, 2]);
        Assert.True(snapshot.RetentionWeight > 0);

        foreach (var capacity in new[] { 0L, snapshot.RetentionWeight - 1 })
        {
            using var fixture = new SnapshotCacheFixture();
            var key = new ArchiveKey(0, 0);
            fixture.SetArchive(key, [1, 2]);
            var cache = fixture.CreateCache(capacity);

            using (cache.ReadMember(key, 0, 1)) { }
            using (cache.ReadMember(key, 0, 1)) { }

            Assert.Equal(2, fixture.GetReadCount(key));
        }
    }

    [Fact]
    public void Constructor_RejectsNegativeCapacity()
    {
        using var fixture = new SnapshotCacheFixture();
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.CreateCache(-1));
    }

    [Fact]
    public async Task ReadMember_SharesAnAttachedColdLoadAndPublishesOnlyOnce()
    {
        var key = new ArchiveKey(0, 1);
        using var fixture = new SnapshotCacheFixture();
        fixture.SetArchive(key, [1, 2, 3]);
        var cache = fixture.CreateCache(1024);
        using var loaderStarted = new ManualResetEventSlim();
        using var releaseLoader = new ManualResetEventSlim();
        using var joinSignal = new FlightJoinSignal();
        fixture.BeforeRead = (_, _) =>
        {
            loaderStarted.Set();
            if (!releaseLoader.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the gated snapshot load.");
            }
        };

        var first = Task.Run(() => cache.ReadMember(key, 0, 1));
        Assert.True(loaderStarted.Wait(TimeSpan.FromSeconds(10)));
        var second = Task.Run(() => cache.ReadMember(key, 0, 1));
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
        Assert.Equal(1, fixture.GetReadCount(key));

        using var retainedStream = cache.ReadMember(key, 0, 1);
        Assert.Equal(1, fixture.GetReadCount(key));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReadMember_SharesUnretainedFlightOnlyWhileCallersAreAttached(long capacityOffset)
    {
        var key = new ArchiveKey(3, 4);
        using var fixture = new SnapshotCacheFixture();
        fixture.SetArchive(key, [7, 8], []);
        using var loaderStarted = new ManualResetEventSlim();
        using var releaseLoader = new ManualResetEventSlim();
        using var joinSignal = new FlightJoinSignal();
        fixture.BeforeRead = (_, _) =>
        {
            loaderStarted.Set();
            if (!releaseLoader.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the gated snapshot load.");
            }
        };
        var capacity = capacityOffset == 0 ? 0 : CreateSnapshot([7, 8], []).RetentionWeight - 1;
        var cache = fixture.CreateCache(capacity);

        var first = Task.Run(() => cache.ReadMember(key, 0, 2));
        Assert.True(loaderStarted.Wait(TimeSpan.FromSeconds(10)));
        var second = Task.Run(() => cache.ReadMember(key, 1, 2));
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
        Assert.Equal(1, fixture.GetReadCount(key));

        using var laterStream = cache.ReadMember(key, 0, 2);
        Assert.Equal(2, fixture.GetReadCount(key));
    }

    [Fact]
    public async Task ReadMember_LoadsDifferentArchivesIndependently()
    {
        var firstKey = new ArchiveKey(0, 1);
        var secondKey = new ArchiveKey(0, 2);
        using var fixture = new SnapshotCacheFixture();
        fixture.SetArchive(firstKey, [1]);
        fixture.SetArchive(secondKey, [2]);
        var cache = fixture.CreateCache(0);
        using var bothLoadsStarted = new CountdownEvent(2);
        using var releaseLoads = new ManualResetEventSlim();
        fixture.BeforeRead = (_, _) =>
        {
            bothLoadsStarted.Signal();
            if (!releaseLoads.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the gated snapshot loads.");
            }
        };

        var first = Task.Run(() => cache.ReadMember(firstKey, 0, 1));
        var second = Task.Run(() => cache.ReadMember(secondKey, 0, 1));
        try
        {
            Assert.True(bothLoadsStarted.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseLoads.Set();
        }

        using var firstStream = await first;
        using var secondStream = await second;
        Assert.Equal(new byte[] { 1 }, firstStream.ToArray());
        Assert.Equal(new byte[] { 2 }, secondStream.ToArray());
    }

    [Fact]
    public void ReadMember_FailedLoadDoesNotPoisonRetryOrRetainFailure()
    {
        var key = new ArchiveKey(0, 0);
        using var fixture = new SnapshotCacheFixture();
        fixture.SetArchiveData(key, [1]);
        var cache = fixture.CreateCache(1024);

        var actual = Assert.Throws<InvalidDataException>(() => cache.ReadMember(key, 0, 2));
        Assert.Contains("footer", actual.Message);

        fixture.SetArchive(key, [9]);
        using var stream = cache.ReadMember(key, 0, 1);

        Assert.Equal(new byte[] { 9 }, stream.ToArray());
        Assert.Equal(2, fixture.GetReadCount(key));
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

    private static MeterListener CreateDurationListener(Action<Instrument> measurement)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == CacheMetrics.MeterName)
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, _, _) => measurement(instrument));
        listener.Start();
        return listener;
    }

    private sealed class SnapshotCacheFixture : IFileStore
    {
        private readonly ConcurrentDictionary<ArchiveKey, byte[]> _files = new();
        private readonly ConcurrentDictionary<ArchiveKey, int> _readCounts = new();

        public int IndexFileCount => 255;
        public Action<int, int>? BeforeRead { get; set; }

        public ArchiveSnapshotCache CreateCache(long capacityBytes) => new(
            capacityBytes,
            new ArchiveSnapshotLoader(this, new ContainerDecoder(), new ArchiveDecoder()));

        public void SetArchive(ArchiveKey key, params byte[][] members) =>
            SetArchiveData(key, ArchiveTestData.EncodeArchive(members));

        public void SetArchiveData(ArchiveKey key, byte[] archiveData)
        {
            using var data = new MemoryStream(archiveData);
            using var container = new Container(data);
            _files[key] = container.Encode();
        }

        public int GetReadCount(ArchiveKey key) => _readCounts.GetValueOrDefault(key);

        public int GetFileCount(int indexId) => _files.Keys.Count(key => key.IndexId == indexId);

        public MemoryStream Read(int indexId, int fileId)
        {
            var key = new ArchiveKey(indexId, fileId);
            BeforeRead?.Invoke(indexId, fileId);
            if (!_files.TryGetValue(key, out var data))
            {
                throw new FileNotFoundException();
            }

            _readCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
            return new MemoryStream(data, writable: false);
        }

        public void Write(int indexId, int fileId, MemoryStream data) =>
            _files[new ArchiveKey(indexId, fileId)] = data.ToArray();

        public void Dispose() { }
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
