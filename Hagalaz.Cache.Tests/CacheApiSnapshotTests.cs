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
    public void ReadReferenceTable_ReturnsOneMemoizedTablePerIndexAndSkipsWarmProviderReads()
    {
        using var fixture = new CacheApiFixture();

        var first = fixture.Cache.ReadReferenceTable(16);
        var warm = fixture.Cache.ReadReferenceTable(16);
        var otherIndex = fixture.Cache.ReadReferenceTable(17);

        Assert.Same(first, warm);
        Assert.NotSame(first, otherIndex);
        Assert.Equal(2, fixture.ReferenceTableReadCount);
    }

    [Fact]
    public void ReadReferenceTable_RetriesAfterProviderFailureWithoutPublishingFailure()
    {
        using var fixture = new CacheApiFixture();
        var failure = new InvalidDataException("corrupt reference table");
        var attempts = 0;
        fixture.BeforeReferenceTableRead = () =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw failure;
            }
        };

        var actual = Assert.Throws<InvalidDataException>(() => fixture.Cache.ReadReferenceTable(16));
        var table = fixture.Cache.ReadReferenceTable(16);
        var warm = fixture.Cache.ReadReferenceTable(16);

        Assert.Same(failure, actual);
        Assert.Same(table, warm);
        Assert.Equal(2, fixture.ReferenceTableReadCount);
    }

    [Fact]
    public async Task ReadReferenceTable_ConcurrentColdLookupsShareOneDecodeForAnIndex()
    {
        const int readerCount = 6;
        using var fixture = new CacheApiFixture();
        using var startGate = new Barrier(readerCount + 1);
        using var readersReady = new CountdownEvent(readerCount);
        using var providerReadStarted = new ManualResetEventSlim();
        using var releaseProviderRead = new ManualResetEventSlim();
        fixture.BeforeReferenceTableRead = () =>
        {
            providerReadStarted.Set();
            if (!releaseProviderRead.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the reference-table decode.");
            }
        };
        var readers = Enumerable.Range(0, readerCount)
            .Select(_ => StartLongRunning(() =>
            {
                if (!startGate.SignalAndWait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The reference-table readers did not reach the start gate.");
                }

                readersReady.Signal();
                return fixture.Cache.ReadReferenceTable(16);
            }))
            .ToArray();

        try
        {
            Assert.True(startGate.SignalAndWait(TimeSpan.FromSeconds(10)));
            Assert.True(readersReady.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(providerReadStarted.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseProviderRead.Set();
        }

        var results = await Task.WhenAll(readers);

        Assert.All(results, result => Assert.Same(results[0], result));
        Assert.Equal(1, fixture.ReferenceTableReadCount);
    }

    [Fact]
    public async Task ReadReferenceTable_ColdLoadsDifferentIndexesInParallel()
    {
        using var fixture = new CacheApiFixture();
        using var bothReadsStarted = new CountdownEvent(2);
        using var releaseReads = new ManualResetEventSlim();
        fixture.BeforeReferenceTableRead = () =>
        {
            bothReadsStarted.Signal();
            if (!releaseReads.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release both reference-table decodes.");
            }
        };

        var first = StartLongRunning(() => fixture.Cache.ReadReferenceTable(16));
        var second = StartLongRunning(() => fixture.Cache.ReadReferenceTable(17));
        try
        {
            Assert.True(bothReadsStarted.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseReads.Set();
        }

        var results = await Task.WhenAll(first, second);

        Assert.NotSame(results[0], results[1]);
        Assert.Equal(2, fixture.ReferenceTableReadCount);
    }

    [Fact]
    public void Write_InvalidatesOnlyAffectedSnapshotAndExistingStreamsStayUsable()
    {
        using var fixture = new CacheApiFixture();
        var oldArchive = ArchiveTestData.EncodeArchive([1, 2], [3, 4]);
        var newArchive = ArchiveTestData.EncodeArchive([8, 9], [10, 11]);
        fixture.SetArchive(8, 1, oldArchive, 2);
        fixture.SetArchive(8, 2, ArchiveTestData.EncodeArchive([5], [6]), 2);
        var cachedTable = fixture.Cache.ReadReferenceTable(8);
        var unrelatedTable = fixture.Cache.ReadReferenceTable(17);

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
        var refreshedTable = fixture.Cache.ReadReferenceTable(8);
        Assert.Equal(new byte[] { 1, 2 }, oldStream.ToArray());
        Assert.Equal(new byte[] { 6 }, unrelated.ToArray());
        Assert.Equal(new byte[] { 8, 9 }, refreshed.ToArray());
        Assert.NotSame(cachedTable, refreshedTable);
        Assert.Same(unrelatedTable, fixture.Cache.ReadReferenceTable(17));
        Assert.Equal(3, fixture.ReferenceTableReadCount);
        Assert.Equal(3, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void Write_InvalidatesSnapshotAfterWriterFailureAndPropagatesOriginalException()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(9, 3, [1, 2], 1);
        var cachedTable = fixture.Cache.ReadReferenceTable(9);
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
        var refreshedTable = fixture.Cache.ReadReferenceTable(9);
        Assert.Equal(new byte[] { 1, 2 }, beforeWrite.ToArray());
        Assert.Equal(new byte[] { 7, 8 }, afterWrite.ToArray());
        Assert.NotSame(cachedTable, refreshedTable);
        Assert.Equal(2, fixture.ReferenceTableReadCount);
        Assert.Equal(2, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public async Task WriteRawFile_WaitsForReferenceTableLoadAndInvalidatesBeforeNextLookup()
    {
        using var fixture = new CacheApiFixture();
        using var firstLookupStarted = new ManualResetEventSlim();
        using var releaseFirstLookup = new ManualResetEventSlim();
        using var rawWriteStarted = new ManualResetEventSlim();
        using var releaseRawWrite = new ManualResetEventSlim();
        using var secondLookupStarted = new ManualResetEventSlim();
        var lookupCount = 0;
        fixture.BeforeReferenceTableRead = () =>
        {
            if (Interlocked.Increment(ref lookupCount) == 1)
            {
                firstLookupStarted.Set();
                if (!releaseFirstLookup.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The test did not release the initial reference-table lookup.");
                }
            }
            else
            {
                secondLookupStarted.Set();
            }
        };
        fixture.RawWriteAction = (_, _, _) =>
        {
            rawWriteStarted.Set();
            if (!releaseRawWrite.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The test did not release the raw table writer.");
            }
        };

        var firstLookup = StartLongRunning(() => fixture.Cache.ReadReferenceTable(16));
        Task? rawWrite = null;
        Task<IReferenceTable>? secondLookup = null;

        try
        {
            Assert.True(firstLookupStarted.Wait(TimeSpan.FromSeconds(10)));
            rawWrite = StartLongRunning(() =>
            {
                using var data = new MemoryStream([7, 8]);
                fixture.Cache.WriteRawFile(255, 16, data);
            });
            Assert.False(rawWriteStarted.Wait(TimeSpan.FromMilliseconds(200)));
            releaseFirstLookup.Set();
            var initialTable = await firstLookup;
            Assert.True(rawWriteStarted.Wait(TimeSpan.FromSeconds(10)));

            secondLookup = StartLongRunning(() => fixture.Cache.ReadReferenceTable(16));
            Assert.False(secondLookupStarted.Wait(TimeSpan.FromMilliseconds(200)));
            releaseRawWrite.Set();

            Assert.True(secondLookupStarted.Wait(TimeSpan.FromSeconds(10)));
            await rawWrite;
            var refreshedTable = await secondLookup;
            Assert.NotSame(initialTable, refreshedTable);
        }
        finally
        {
            releaseFirstLookup.Set();
            releaseRawWrite.Set();
            await firstLookup;
            if (rawWrite is not null)
            {
                await rawWrite;
            }
            if (secondLookup is not null)
            {
                await secondLookup;
            }
        }

        Assert.Equal(2, fixture.ReferenceTableReadCount);
    }

    [Fact]
    public void WriteRawFile_InvalidatesSnapshotAfterStoreFailureAndPropagatesOriginalException()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(9, 3, [1, 2], 1);
        var cachedTable = fixture.Cache.ReadReferenceTable(9);
        using var beforeWrite = fixture.Cache.Read(9, 3, 0);
        var failure = new IOException("raw file write failed");
        fixture.RawWriteAction = (indexId, archiveId, _) =>
        {
            fixture.SetArchive(indexId, archiveId, [7, 8], 1);
            throw failure;
        };

        using var data = new MemoryStream([7, 8]);
        var actual = Assert.Throws<IOException>(() => fixture.Cache.WriteRawFile(9, 3, data));

        using var afterWrite = fixture.Cache.Read(9, 3, 0);
        var stillCachedTable = fixture.Cache.ReadReferenceTable(9);
        Assert.Same(failure, actual);
        Assert.Same(cachedTable, stillCachedTable);
        Assert.Equal(1, fixture.ReferenceTableReadCount);
        Assert.Equal(new byte[] { 1, 2 }, beforeWrite.ToArray());
        Assert.Equal(new byte[] { 7, 8 }, afterWrite.ToArray());
        Assert.Equal(2, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void WriteRawFile_ReferenceTableTargetClearsItsTableAndAllIndexSnapshots()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(16, 1, ArchiveTestData.EncodeArchive([1], [2]), 2);
        fixture.SetArchive(16, 2, ArchiveTestData.EncodeArchive([3], [4]), 2);
        fixture.SetArchive(17, 1, [5], 1);
        using (fixture.Cache.Read(16, 1, 0)) { }
        using (fixture.Cache.Read(16, 2, 0)) { }
        using (fixture.Cache.Read(17, 1, 0)) { }
        var index16Table = fixture.Cache.ReadReferenceTable(16);
        var otherIndexTable = fixture.Cache.ReadReferenceTable(17);

        using var rawTable = new MemoryStream([7, 8]);
        fixture.Cache.WriteRawFile(255, 16, rawTable);

        AssertReferenceTableMutationInvalidatedIndex16(fixture, index16Table, otherIndexTable);
    }

    [Fact]
    public void CopyRawFile_PreservesSourceAndInvalidatesOnlyDestinationSnapshot()
    {
        using var fixture = new CacheApiFixture();
        var destinationArchive = ArchiveTestData.EncodeArchive([1, 2], [3]);
        var sourceArchive = ArchiveTestData.EncodeArchive([8, 9], [10, 11]);
        fixture.SetArchive(8, 1, destinationArchive, 2);
        fixture.SetArchive(8, 2, sourceArchive, 2);
        var cachedTable = fixture.Cache.ReadReferenceTable(8);
        using var sourceBefore = fixture.Cache.Read(8, 2, 0);
        using var destinationBefore = fixture.Cache.Read(8, 1, 0);

        fixture.Cache.CopyRawFile(8, 1, 8, 2);

        Assert.Same(fixture.LastStoreReadStream, fixture.LastStoreWriteStream);
        Assert.Equal(sourceArchive, fixture.GetStoredBytes(8, 1));
        Assert.Equal(sourceArchive, fixture.GetStoredBytes(8, 2));
        Assert.Same(cachedTable, fixture.Cache.ReadReferenceTable(8));
        using var sourceAfter = fixture.Cache.Read(8, 2, 1);
        using var destinationAfter = fixture.Cache.Read(8, 1, 0);
        Assert.Equal(new byte[] { 8, 9 }, sourceBefore.ToArray());
        Assert.Equal(new byte[] { 10, 11 }, sourceAfter.ToArray());
        Assert.Equal(new byte[] { 8, 9 }, destinationAfter.ToArray());
        Assert.Equal(new byte[] { 1, 2 }, destinationBefore.ToArray());
        Assert.Equal(3, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void CopyRawFile_SupportsSelfCopyAndInvalidatesDestinationSnapshot()
    {
        using var fixture = new CacheApiFixture();
        var archive = ArchiveTestData.EncodeArchive([4, 5], [6]);
        fixture.SetArchive(8, 1, archive, 2);
        using var beforeCopy = fixture.Cache.Read(8, 1, 0);

        fixture.Cache.CopyRawFile(8, 1, 8, 1);

        using var afterCopy = fixture.Cache.Read(8, 1, 1);
        Assert.Equal(archive, fixture.GetStoredBytes(8, 1));
        Assert.Equal(new byte[] { 4, 5 }, beforeCopy.ToArray());
        Assert.Equal(new byte[] { 6 }, afterCopy.ToArray());
        Assert.Equal(2, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void CopyRawFile_InvalidatesDestinationAfterStoreFailureAndPropagatesOriginalException()
    {
        using var fixture = new CacheApiFixture();
        var updatedArchive = ArchiveTestData.EncodeArchive([7], [8]);
        fixture.SetArchive(8, 1, ArchiveTestData.EncodeArchive([1], [2]), 2);
        fixture.SetArchive(8, 2, ArchiveTestData.EncodeArchive([3], [4]), 2);
        using var beforeCopy = fixture.Cache.Read(8, 1, 0);
        var failure = new IOException("raw copy failed");
        fixture.RawWriteAction = (indexId, archiveId, _) =>
        {
            fixture.SetArchive(indexId, archiveId, updatedArchive, 2);
            throw failure;
        };

        var actual = Assert.Throws<IOException>(() => fixture.Cache.CopyRawFile(8, 1, 8, 2));

        using var afterCopy = fixture.Cache.Read(8, 1, 0);
        Assert.Same(failure, actual);
        Assert.Equal(new byte[] { 1 }, beforeCopy.ToArray());
        Assert.Equal(new byte[] { 7 }, afterCopy.ToArray());
        Assert.Equal(2, fixture.ArchiveDecodeCount);
    }

    [Fact]
    public void CopyRawFile_ReferenceTableTargetClearsTableAndIndexSnapshots()
    {
        using var fixture = new CacheApiFixture();
        fixture.SetArchive(16, 1, [1], 1);
        fixture.SetArchive(16, 2, [2], 1);
        fixture.SetArchive(17, 1, [3], 1);
        fixture.SetArchive(255, 17, [7, 8], 0);
        using (fixture.Cache.Read(16, 1, 0)) { }
        using (fixture.Cache.Read(16, 2, 0)) { }
        using (fixture.Cache.Read(17, 1, 0)) { }
        var originalTable = fixture.Cache.ReadReferenceTable(16);
        var otherIndexTable = fixture.Cache.ReadReferenceTable(17);

        fixture.Cache.CopyRawFile(255, 16, 255, 17);

        Assert.Equal(new byte[] { 7, 8 }, fixture.GetStoredBytes(255, 16));
        AssertReferenceTableMutationInvalidatedIndex16(fixture, originalTable, otherIndexTable);
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

    private static void AssertReferenceTableMutationInvalidatedIndex16(
        CacheApiFixture fixture,
        IReferenceTable originalTable,
        IReferenceTable otherIndexTable)
    {
        Assert.NotSame(originalTable, fixture.Cache.ReadReferenceTable(16));
        Assert.Same(otherIndexTable, fixture.Cache.ReadReferenceTable(17));
        using (fixture.Cache.Read(16, 1, 0)) { }
        using (fixture.Cache.Read(16, 2, 0)) { }
        using (fixture.Cache.Read(17, 1, 0)) { }
        Assert.Equal(5, fixture.ArchiveDecodeCount);
        Assert.Equal(3, fixture.ReferenceTableReadCount);
    }

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
        private int _referenceTableReadCount;
        private MemoryStream? _lastStoreReadStream;
        private MemoryStream? _lastStoreWriteStream;

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

                    var stream = new MemoryStream(bytes, writable: false);
                    Volatile.Write(ref _lastStoreReadStream, stream);
                    return stream;
                });
            _store.Setup(store => store.Write(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MemoryStream>()))
                .Callback<int, int, MemoryStream>((indexId, fileId, data) =>
                {
                    Volatile.Write(ref _lastStoreWriteStream, data);
                    RawWriteAction?.Invoke(indexId, fileId, data);
                    _archiveBytes[(indexId, fileId)] = data.ToArray();
                });
            _referenceTableProvider.Setup(provider => provider.ReadReferenceTable(It.IsAny<int>()))
                .Returns((int indexId) =>
                {
                    Interlocked.Increment(ref _referenceTableReadCount);
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
        public int ReferenceTableReadCount => Volatile.Read(ref _referenceTableReadCount);
        public MemoryStream? LastStoreReadStream => Volatile.Read(ref _lastStoreReadStream);
        public MemoryStream? LastStoreWriteStream => Volatile.Read(ref _lastStoreWriteStream);
        public Action? BeforeContainerDecode { get; set; }
        public Action? BeforeReferenceTableRead { get; set; }
        public Action<int, int, IContainer>? WriteAction { get; set; }
        public Action<int, int, MemoryStream>? RawWriteAction { get; set; }

        public void SetArchive(int indexId, int archiveId, byte[] data, int memberCount)
        {
            _archiveBytes[(indexId, archiveId)] = data;
            _memberCounts[(indexId, archiveId)] = memberCount;
        }

        public byte[] GetStoredBytes(int indexId, int archiveId) => _archiveBytes[(indexId, archiveId)];

        public void Dispose() => Cache.Dispose();
    }
}
