using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
using Hagalaz.Cache.Utilities;
using Hagalaz.TestSupport;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class CacheWriterIntegrationTests
{
    [Fact]
    public void Write_RefreshesMemoizedTableAndSnapshotWithPersistedVersionAndChecksum()
    {
        var store = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        var initialArchiveBytes = ArchiveTestData.EncodeArchive([1, 2, 3]);
        SeedCache(store, tableCodec, initialArchiveBytes);
        using var cache = CreateCache(store);

        var cachedTable = cache.ReadReferenceTable(16);
        var cachedEntry = cachedTable.GetEntry(1)!;
        using var beforeWrite = cache.Read(16, 1, 0);
        Assert.Equal(initialArchiveBytes, beforeWrite.ToArray());
        Assert.Equal(1, store.GetReadCount(255, 16));

        var replacementArchiveBytes = ArchiveTestData.EncodeArchive([9, 8, 7]);
        using var replacementContainer = new Container(CompressionType.None, new MemoryStream(replacementArchiveBytes), 5);
        cache.Write(16, 1, replacementContainer);

        Assert.Equal(5, cachedTable.Version);
        Assert.Equal(4, cachedEntry.Version);
        Assert.Equal(123, cachedEntry.Crc32);
        Assert.Equal(3, store.GetReadCount(255, 16));
        Assert.Equal(replacementContainer.Encode(false), store.GetBytes(16, 1));

        var refreshedTable = cache.ReadReferenceTable(16);
        var refreshedEntry = refreshedTable.GetEntry(1)!;
        Assert.NotSame(cachedTable, refreshedTable);
        Assert.Equal(6, refreshedTable.Version);
        Assert.Equal(6, refreshedEntry.Version);
        Assert.Equal(
            BufferUtilities.GetCrcChecksum(store.GetBytes(16, 1), 0, store.GetBytes(16, 1).Length),
            refreshedEntry.Crc32);
        Assert.Equal(4, store.GetReadCount(255, 16));

        using var afterWrite = cache.Read(16, 1, 0);
        Assert.Equal(initialArchiveBytes, beforeWrite.ToArray());
        Assert.Equal(replacementArchiveBytes, afterWrite.ToArray());
        Assert.Equal(2, store.GetReadCount(16, 1));
    }

    [Fact]
    public void Write_WhenReferenceTablePersistenceFailsDoesNotMutateCachedTableObject()
    {
        var backingStore = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        var initialArchiveBytes = ArchiveTestData.EncodeArchive([1, 2]);
        SeedCache(backingStore, tableCodec, initialArchiveBytes);
        var failure = new IOException("reference-table persistence failed");
        var store = new Mock<IFileStore>();
        store.SetupGet(current => current.IndexFileCount).Returns(backingStore.IndexFileCount);
        store.Setup(current => current.Read(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int indexId, int fileId) => backingStore.Read(indexId, fileId));
        store.Setup(current => current.Write(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MemoryStream>()))
            .Callback<int, int, MemoryStream>((indexId, fileId, data) =>
            {
                if (indexId == 255 && fileId == 16)
                {
                    throw failure;
                }

                backingStore.Write(indexId, fileId, data);
            });
        store.Setup(current => current.Dispose()).Callback(backingStore.Dispose);
        using var cache = CreateCache(store.Object);

        var cachedTable = cache.ReadReferenceTable(16);
        var cachedEntry = cachedTable.GetEntry(1)!;
        using var beforeWrite = cache.Read(16, 1, 0);
        var replacementArchiveBytes = ArchiveTestData.EncodeArchive([7, 8]);
        using var replacementContainer = new Container(CompressionType.None, new MemoryStream(replacementArchiveBytes), 9);

        var actual = Assert.Throws<IOException>(() => cache.Write(16, 1, replacementContainer));

        var refreshedTable = cache.ReadReferenceTable(16);
        var refreshedEntry = refreshedTable.GetEntry(1)!;
        using var afterWrite = cache.Read(16, 1, 0);
        Assert.Same(failure, actual);
        Assert.NotSame(cachedTable, refreshedTable);
        Assert.Equal(5, cachedTable.Version);
        Assert.Equal(4, cachedEntry.Version);
        Assert.Equal(123, cachedEntry.Crc32);
        Assert.Equal(5, refreshedTable.Version);
        Assert.Equal(4, refreshedEntry.Version);
        Assert.Equal(123, refreshedEntry.Crc32);
        Assert.Equal(initialArchiveBytes, beforeWrite.ToArray());
        Assert.Equal(replacementArchiveBytes, afterWrite.ToArray());
    }

    [Fact]
    public void Write_WhenArchiveEntryIsMissing_RejectsBeforeWritingAnything()
    {
        var store = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        SeedCache(store, tableCodec, ArchiveTestData.EncodeArchive([1, 2]));
        var originalReferenceTableBytes = store.GetBytes(255, 16);
        using var cache = CreateCache(store);
        using var data = new MemoryStream([9, 8, 7]);
        using var container = new Container(CompressionType.None, data, 5);

        var exception = Assert.Throws<IOException>(() => cache.Write(16, 2, container));

        Assert.Contains("reference-table entry", exception.Message);
        Assert.Equal((short)5, container.Version);
        Assert.Throws<FileNotFoundException>(() => store.Read(16, 2));
        Assert.Equal(originalReferenceTableBytes, store.GetBytes(255, 16));
        Assert.Null(cache.ReadReferenceTable(16).GetEntry(2));
    }

    [Fact]
    public async Task ConcurrentColdLookupsReadOnePhysicalReferenceTablePerIndex()
    {
        const int readerCount = 8;
        var store = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        SeedCache(store, tableCodec, ArchiveTestData.EncodeArchive([1]));
        using var decoderStarted = new ManualResetEventSlim();
        using var releaseDecoder = new ManualResetEventSlim();
        var realDecoder = new ContainerDecoder();
        var containerDecoder = new Mock<IContainerDecoder>();
        containerDecoder.Setup(current => current.Decode(It.IsAny<MemoryStream>()))
            .Returns((MemoryStream stream) =>
            {
                decoderStarted.Set();
                if (!releaseDecoder.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The test did not release the reference-table container decode.");
                }

                return realDecoder.Decode(stream);
            });
        var referenceTableProvider = new ReferenceTableProvider(store, containerDecoder.Object, tableCodec);
        var cacheWriter = new Mock<ICacheWriter>().Object;
        using var cache = new CacheApi(
            store,
            referenceTableProvider,
            cacheWriter,
            containerDecoder.Object,
            tableCodec,
            new ArchiveDecoder(),
            Options.Create(new CacheOptions()));
        using var startGate = new Barrier(readerCount + 1);
        using var readersReady = new CountdownEvent(readerCount);
        var readers = Enumerable.Range(0, readerCount)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    if (!startGate.SignalAndWait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The reference-table readers did not reach the start gate.");
                    }

                    readersReady.Signal();
                    return cache.ReadReferenceTable(16);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        try
        {
            Assert.True(startGate.SignalAndWait(TimeSpan.FromSeconds(10)));
            Assert.True(readersReady.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(decoderStarted.Wait(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            releaseDecoder.Set();
        }

        var results = await Task.WhenAll(readers);

        Assert.All(results, result => Assert.Same(results[0], result));
        Assert.Equal(1, store.GetReadCount(255, 16));
    }

    private static CacheApi CreateCache(IFileStore store)
    {
        var containerDecoder = new ContainerDecoder();
        var tableCodec = new ReferenceTableCodec();
        var tableProvider = new ReferenceTableProvider(store, containerDecoder, tableCodec);
        var writer = new CacheWriter(store, tableProvider, containerDecoder, tableCodec);
        return new CacheApi(
            store,
            tableProvider,
            writer,
            containerDecoder,
            tableCodec,
            new ArchiveDecoder(),
            Options.Create(new CacheOptions()));
    }

    private static void SeedCache(MemoryFileStore store, ReferenceTableCodec tableCodec, byte[] archiveBytes)
    {
        var table = new ReferenceTable(7, 5, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(1)
        {
            Version = 4,
            Crc32 = 123
        };
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(1, entry);

        using var encodedTable = tableCodec.Encode(table);
        store.Set(255, 16, EncodeContainer(encodedTable.ToArray()));
        store.Set(16, 1, EncodeContainer(archiveBytes));
    }

    private static byte[] EncodeContainer(byte[] data)
    {
        using var content = new MemoryStream(data);
        using var container = new Container(content);
        return container.Encode();
    }
}
