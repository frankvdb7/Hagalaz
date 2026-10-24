using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

[Collection(CacheApiTelemetryCollection.Name)]
public sealed class CacheApiSnapshotTests
{
    [Fact]
    public void Read_ReusesDecodedArchiveAndReturnsRequestedPhysicalMember()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(16, 7, ArchiveTestData.EncodeArchive([1, 2, 3], [4, 5], [6, 7, 8, 9]), 3);

        using var first = fixture.Cache.Read(16, 7, 2);
        using var second = fixture.Cache.Read(16, 7, 0);

        Assert.Equal(new byte[] { 6, 7, 8, 9 }, first.ToArray());
        Assert.Equal(new byte[] { 1, 2, 3 }, second.ToArray());
        Assert.Equal(1, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void Read_PreservesMissingAndCorruptArchiveFailuresAndReleasesReadLock()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(5, 1, [1, 2], 1);

        Assert.Throws<FileNotFoundException>(() => fixture.Cache.Read(255, 1, 0));
        Assert.Throws<FileNotFoundException>(() => fixture.Cache.Read(5, 99, 0));
        Assert.Throws<FileNotFoundException>(() => fixture.Cache.Read(5, 1, 1));

        fixture.SetArchive(5, 2, [1], 2);
        Assert.Throws<InvalidDataException>(() => fixture.Cache.Read(5, 2, 0));

        using var valid = fixture.Cache.Read(5, 1, 0);
        Assert.Equal(new byte[] { 1, 2 }, valid.ToArray());
    }

    [Fact]
    public void Write_InvalidatesOnlyAffectedSnapshotAndExistingStreamsStayUsable()
    {
        using var fixture = new CacheApiFixture();
        var oldArchive = ArchiveTestData.EncodeArchive([1, 2], [3, 4]);
        var newArchive = ArchiveTestData.EncodeArchive([8, 9], [10, 11]);
        fixture.SetArchive(8, 1, oldArchive, 2);
        fixture.SetArchive(8, 2, ArchiveTestData.EncodeArchive([5], [6]), 2);

        using var oldStream = fixture.Cache.Read(8, 1, 0);
        using (fixture.Cache.Read(8, 2, 0)) { }
        Assert.Equal(2, fixture.ArchiveDecodeCount);

        fixture.WriteAction = (indexId, archiveId, _) => fixture.SetArchive(indexId, archiveId, newArchive, 2);
        using (var container = new Container(new MemoryStream()))
        {
            fixture.Cache.Write(8, 1, container);
        }

        using var unrelated = fixture.Cache.Read(8, 2, 1);
        using var refreshed = fixture.Cache.Read(8, 1, 0);
        Assert.Equal(new byte[] { 1, 2 }, oldStream.ToArray());
        Assert.Equal(new byte[] { 6 }, unrelated.ToArray());
        Assert.Equal(new byte[] { 8, 9 }, refreshed.ToArray());
        Assert.Equal(3, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void Write_InvalidatesSnapshotAfterWriterFailureAndPropagatesOriginalException()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(9, 3, [1, 2], 1);
        using var beforeWrite = fixture.Cache.Read(9, 3, 0);
        var failure = new IOException("reference table write failed");
        fixture.WriteAction = (indexId, archiveId, _) =>
        {
            fixture.SetArchive(indexId, archiveId, [7, 8], 1);
            throw failure;
        };

        using (var container = new Container(new MemoryStream()))
        {
            var actual = Assert.Throws<IOException>(() => fixture.Cache.Write(9, 3, container));
            Assert.Same(failure, actual);
        }

        using var afterWrite = fixture.Cache.Read(9, 3, 0);
        Assert.Equal(new byte[] { 1, 2 }, beforeWrite.ToArray());
        Assert.Equal(new byte[] { 7, 8 }, afterWrite.ToArray());
        Assert.Equal(2, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void ReadArchive_RemainsDetachedMutableAndCannotChangeSnapshotBytes()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(10, 4, ArchiveTestData.EncodeArchive([1, 2, 3], [4, 5]), 2);
        using var snapshotRead = fixture.Cache.Read(10, 4, 0);
        using var firstArchive = fixture.Cache.ReadArchive(10, 4);
        using var secondArchive = fixture.Cache.ReadArchive(10, 4);

        firstArchive.GetEntry(0).WriteByte(99);

        Assert.Equal(new byte[] { 1, 2, 3 }, snapshotRead.ToArray());
        Assert.Equal(new byte[] { 1, 2, 3 }, secondArchive.GetEntry(0).ToArray());
        Assert.NotSame(firstArchive, secondArchive);

        using var laterSnapshotRead = fixture.Cache.Read(10, 4, 0);
        Assert.Equal(new byte[] { 1, 2, 3 }, laterSnapshotRead.ToArray());
    }

    [Fact]
    public async Task Write_ExcludesColdReadsAndNewReadsUntilExactInvalidationCompletes()
    {
        using var fixture = new CacheApiFixture();
        var oldArchive = new byte[] { 1, 2, 3 };
        var newArchive = new byte[] { 8, 9, 10 };
        fixture.SetArchive(12, 5, oldArchive, 1);
        using var coldLoadStarted = new ManualResetEventSlim();
        using var releaseColdLoad = new ManualResetEventSlim();
        using var writerStarted = new ManualResetEventSlim();
        using var releaseWriter = new ManualResetEventSlim();
        using var referenceReadReached = new ManualResetEventSlim();
        fixture.BeforeContainerDecode = () =>
        {
            coldLoadStarted.Set();
            if (!releaseColdLoad.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the cold archive load.");
            }
        };
        fixture.WriteAction = (indexId, archiveId, _) =>
        {
            fixture.SetArchive(indexId, archiveId, newArchive, 1);
            writerStarted.Set();
            if (!releaseWriter.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the active writer.");
            }
        };

        var coldRead = StartLongRunning(() => fixture.Cache.Read(12, 5, 0));
        Assert.True(coldLoadStarted.Wait(TimeSpan.FromSeconds(10)));
        var writer = StartLongRunning(() => Write(fixture, 12, 5));
        Task<MemoryStream>? postWriteRead = null;

        try
        {
            Assert.False(writerStarted.Wait(TimeSpan.FromMilliseconds(200)));
            releaseColdLoad.Set();
            using var oldStream = await coldRead;
            Assert.Equal(oldArchive, oldStream.ToArray());
            Assert.True(writerStarted.Wait(TimeSpan.FromSeconds(10)));

            fixture.BeforeReferenceTableRead = () => referenceReadReached.Set();
            postWriteRead = StartLongRunning(() => fixture.Cache.Read(12, 5, 0));
            Assert.False(referenceReadReached.Wait(TimeSpan.FromMilliseconds(200)));

            releaseWriter.Set();
            Assert.True(referenceReadReached.Wait(TimeSpan.FromSeconds(10)));
            await writer;

            using var newStream = await postWriteRead;
            Assert.Equal(newArchive, newStream.ToArray());
            Assert.Equal(2, fixture.ArchiveDecodeCount);
        }
        finally
        {
            releaseColdLoad.Set();
            releaseWriter.Set();
            if (postWriteRead is not null)
            {
                await postWriteRead;
            }
        }

        static void Write(CacheApiFixture currentFixture, int indexId, int archiveId)
        {
            using var container = new Container(new MemoryStream());
            currentFixture.Cache.Write(indexId, archiveId, container);
        }
    }

    [Fact]
    public async Task Write_SerializesMultipleWriters()
    {
        using var fixture = new CacheApiFixture();
        using var firstWriterStarted = new ManualResetEventSlim();
        using var releaseFirstWriter = new ManualResetEventSlim();
        using var secondWriterStarted = new ManualResetEventSlim();
        var activeWriters = 0;
        var maximumActiveWriters = 0;
        fixture.WriteAction = (_, archiveId, _) =>
        {
            var active = Interlocked.Increment(ref activeWriters);
            UpdateMaximum(ref maximumActiveWriters, active);
            if (archiveId == 1)
            {
                firstWriterStarted.Set();
                if (!releaseFirstWriter.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The test did not release the first writer.");
                }
            }
            else
            {
                secondWriterStarted.Set();
            }

            Interlocked.Decrement(ref activeWriters);
        };

        var first = StartLongRunning(() => Write(fixture, 1));
        Assert.True(firstWriterStarted.Wait(TimeSpan.FromSeconds(10)));
        var second = StartLongRunning(() => Write(fixture, 2));
        try
        {
            Assert.False(secondWriterStarted.Wait(TimeSpan.FromMilliseconds(200)));
        }
        finally
        {
            releaseFirstWriter.Set();
        }

        await Task.WhenAll(first, second);
        Assert.True(secondWriterStarted.IsSet);
        Assert.Equal(1, maximumActiveWriters);

        static void Write(CacheApiFixture currentFixture, int archiveId)
        {
            using var container = new Container(new MemoryStream());
            currentFixture.Cache.Write(1, archiveId, container);
        }
    }

    private static Task<T> StartLongRunning<T>(Func<T> action) => Task.Factory.StartNew(
        action,
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);

    private static Task StartLongRunning(Action action) => Task.Factory.StartNew(
        action,
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);

    private static void UpdateMaximum(ref int maximum, int value)
    {
        var current = Volatile.Read(ref maximum);
        while (value > current)
        {
            var prior = Interlocked.CompareExchange(ref maximum, value, current);
            if (prior == current)
            {
                return;
            }

            current = prior;
        }
    }

    private sealed class CacheApiFixture : IDisposable
    {
        private readonly Mock<IFileStore> _store = new();
        private readonly Mock<IReferenceTableProvider> _referenceTableProvider = new();
        private readonly Mock<ICacheWriter> _writer = new();
        private readonly Mock<IContainerDecoder> _containerDecoder = new();
        private readonly Mock<IReferenceTableCodec> _referenceTableCodec = new();
        private readonly Mock<IArchiveDecoder> _archiveDecoder = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<(int IndexId, int ArchiveId), byte[]> _archiveBytes = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<(int IndexId, int ArchiveId), int> _memberCounts = new();
        private int _archiveDecodeCount;

        public CacheApiFixture(long capacityBytes = 1024 * 1024)
        {
            _store.SetupGet(store => store.IndexFileCount).Returns(255);
            _store.Setup(store => store.Read(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int indexId, int archiveId) =>
                {
                    if (!_archiveBytes.TryGetValue((indexId, archiveId), out var bytes))
                    {
                        throw new FileNotFoundException();
                    }

                    return new MemoryStream(bytes, writable: false);
                });
            _referenceTableProvider.Setup(provider => provider.ReadReferenceTable(It.IsAny<int>()))
                .Returns((int indexId) =>
                {
                    BeforeReferenceTableRead?.Invoke();
                    var table = new Mock<IReferenceTable>();
                    table.SetupGet(referenceTable => referenceTable.Capacity).Returns(4096);
                    table.Setup(referenceTable => referenceTable.GetEntry(It.IsAny<int>()))
                        .Returns((int archiveId) =>
                        {
                            if (!_memberCounts.TryGetValue((indexId, archiveId), out var memberCount))
                            {
                                return null;
                            }

                            var entry = new Mock<IReferenceTableEntry>();
                            entry.SetupGet(reference => reference.Capacity).Returns(memberCount);
                            return entry.Object;
                        });
                    return table.Object;
                });
            _containerDecoder.Setup(decoder => decoder.Decode(It.IsAny<MemoryStream>()))
                .Returns((MemoryStream stream) =>
                {
                    BeforeContainerDecode?.Invoke();
                    return new Container(new MemoryStream(stream.ToArray()));
                });
            _archiveDecoder.Setup(decoder => decoder.Decode(It.IsAny<IContainer>(), It.IsAny<int>()))
                .Returns((IContainer container, int memberCount) =>
                {
                    Interlocked.Increment(ref _archiveDecodeCount);
                    return new ArchiveDecoder().Decode(container, memberCount);
                });
            _writer.Setup(writer => writer.Write(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IContainer>()))
                .Callback<int, int, IContainer>((indexId, archiveId, container) => WriteAction?.Invoke(indexId, archiveId, container));

            Cache = new CacheApi(
                _store.Object,
                _referenceTableProvider.Object,
                _writer.Object,
                _containerDecoder.Object,
                _referenceTableCodec.Object,
                _archiveDecoder.Object,
                Options.Create(new CacheOptions { ArchiveSnapshotCacheSizeBytes = capacityBytes }));
        }

        public CacheApi Cache { get; }
        public int ArchiveDecodeCount => Volatile.Read(ref _archiveDecodeCount);
        public Action? BeforeContainerDecode { get; set; }
        public Action? BeforeReferenceTableRead { get; set; }
        public Action<int, int, IContainer>? WriteAction { get; set; }

        public void SetArchive(int indexId, int archiveId, byte[] data, int memberCount)
        {
            _archiveBytes[(indexId, archiveId)] = data;
            _memberCounts[(indexId, archiveId)] = memberCount;
        }

        public void Dispose() => Cache.Dispose();
    }
}
