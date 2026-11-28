using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
using Hagalaz.Cache.Utilities;
using Hagalaz.Security;
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
        using var store = new MemoryFileStore();
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
        Assert.Equal(5, refreshedEntry.Version);
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
    public void ReplaceArchiveMember_AdvancesReferenceTableVersionAcrossTrailerFreeWrites()
    {
        using var store = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        SeedCache(store, tableCodec, ArchiveTestData.EncodeArchive([1, 2, 3]), version: 5);
        using var cache = CreateCache(store);
        Assert.Equal(5, cache.ReadReferenceTable(16).GetEntry(1)!.Version);
        using (var initialContainer = cache.ReadContainer(16, 1))
        {
            Assert.Equal((short)5, initialContainer.Version);
        }

        cache.ReplaceArchiveMember(16, 1, 0, new byte[] { 9, 8 });

        Assert.Equal(6, cache.ReadReferenceTable(16).GetEntry(1)!.Version);
        AssertTrailerFreeArchive(store);
        using (var firstMember = cache.Read(16, 1, 0))
        {
            Assert.Equal(new byte[] { 9, 8 }, firstMember.ToArray());
        }

        cache.ReplaceArchiveMember(16, 1, 0, new byte[] { 7, 6 });

        var updatedTable = cache.ReadReferenceTable(16);
        Assert.Equal(7, updatedTable.GetEntry(1)!.Version);
        Assert.Equal(7, updatedTable.Version);
        AssertTrailerFreeArchive(store);
        using var finalMember = cache.Read(16, 1, 0);
        Assert.Equal(new byte[] { 7, 6 }, finalMember.ToArray());
    }

    [Fact]
    public void Write_WhenReferenceTablePersistenceFailsDoesNotMutateCachedTableObject()
    {
        using var backingStore = new MemoryFileStore();
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
        using var storeObject = store.Object;
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
        using var store = new MemoryFileStore();
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
    public async Task CreateChecksumTable_WaitsForConcurrentArchiveMutationToFinish()
    {
        using var backingStore = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        SeedCache(backingStore, tableCodec, ArchiveTestData.EncodeArchive([1, 2, 3]));
        var originalReferenceTable = backingStore.GetBytes(255, 16);
        using var store = new BlockingChecksumFileStore(backingStore);
        using var cache = CreateCache(store);
        _ = cache.ReadReferenceTable(16);
        store.BlockNextChecksumRead();

        var checksumTask = Task.Factory.StartNew(
            cache.CreateChecksumTable,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Task? mutationTask = null;

        try
        {
            Assert.True(store.ChecksumReferenceReadEntered.Wait(TimeSpan.FromSeconds(10)));
            using var mutationStarted = new ManualResetEventSlim();
            mutationTask = Task.Factory.StartNew(
                () =>
                {
                    mutationStarted.Set();
                    cache.ReplaceArchiveMember(16, 1, 0, new byte[] { 9, 8, 7 });
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Assert.True(mutationStarted.Wait(TimeSpan.FromSeconds(10)));
            Assert.False(
                store.ArchiveWriteEntered.Wait(TimeSpan.FromMilliseconds(200)),
                "The archive mutation must wait until checksum generation releases its read lock.");
        }
        finally
        {
            store.ReleaseChecksumReferenceRead.Set();
            await checksumTask;
            if (mutationTask is not null)
            {
                await mutationTask;
            }
        }

        var checksumTable = await checksumTask;
        Assert.Equal(
            BufferUtilities.GetCrcChecksum(originalReferenceTable, 0, originalReferenceTable.Length),
            checksumTable[16].Crc32);
        Assert.True(store.ArchiveWriteEntered.IsSet);
    }

    [Fact]
    public async Task ConcurrentColdLookupsReadOnePhysicalReferenceTablePerIndex()
    {
        const int readerCount = 8;
        using var store = new MemoryFileStore();
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

    private static readonly int[] Keys = [0x1234567, -42, 0x7fffffff, 99];

    [Fact]
    public void TryReplaceContainer_WithKeys_StoresEncryptedBytesThatReopenAndMatchTheReferenceTable()
    {
        using var store = new MemoryFileStore();
        var tableCodec = new ReferenceTableCodec();
        SeedCache(store, tableCodec, ArchiveTestData.EncodeArchive([1, 2, 3]));
        var original = Enumerable.Range(0, 200).Select(i => (byte)(i * 7)).ToArray();
        store.Set(16, 1, EncryptedGzip(original, Keys));
        using var cache = CreateCache(store);
        var untouched = store.GetBytes(255, 16);
        var replacement = Enumerable.Range(0, 300).Select(i => (byte)(i * 3 + 1)).ToArray();
        using var container = new Container(CompressionType.Gzip, new MemoryStream(replacement));

        Assert.True(cache.TryReplaceContainer(16, 1, original, container, Keys));

        var stored = store.GetBytes(16, 1);
        Assert.NotEqual(container.Encode(false), stored);
        using (var reopened = cache.ReadContainer(16, 1, Keys)) Assert.Equal(replacement, reopened.Data.ToArray());
        Assert.ThrowsAny<Exception>(() => cache.ReadContainer(16, 1));
        Assert.ThrowsAny<Exception>(() => cache.ReadContainer(16, 1, [1, 2, 3, 4]));
        var entry = cache.ReadReferenceTable(16).GetEntry(1)!;
        Assert.Equal(BufferUtilities.GetCrcChecksum(stored, 0, stored.Length), entry.Crc32);
        Assert.Equal(5, entry.Version);
        Assert.NotEqual(untouched, store.GetBytes(255, 16));
    }

    [Fact]
    public void TryReplaceContainer_WithKeys_RecordsTheDigestOfTheEncryptedBytes()
    {
        using var store = new MemoryFileStore();
        SeedCache(store, new ReferenceTableCodec(), ArchiveTestData.EncodeArchive([1]), flags: ReferenceTableFlags.Digests);
        var original = new byte[100];
        store.Set(16, 1, EncryptedGzip(original, Keys));
        using var cache = CreateCache(store);
        using var container = new Container(CompressionType.Gzip, new MemoryStream([9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 9, 8]));

        Assert.True(cache.TryReplaceContainer(16, 1, original, container, Keys));

        var stored = store.GetBytes(16, 1);
        var entry = cache.ReadReferenceTable(16).GetEntry(1)!;
        Assert.Equal(Whirlpool.GenerateDigest(stored, 0, stored.Length), entry.WhirlpoolDigest);
        var plain = container.Encode(false);
        Assert.NotEqual(Whirlpool.GenerateDigest(plain, 0, plain.Length), entry.WhirlpoolDigest);
    }

    [Fact]
    public void TryReplaceContainer_WithWrongKeys_WritesNothing()
    {
        using var store = new MemoryFileStore();
        SeedCache(store, new ReferenceTableCodec(), ArchiveTestData.EncodeArchive([1]));
        var original = new byte[100];
        store.Set(16, 1, EncryptedGzip(original, Keys));
        using var cache = CreateCache(store);
        var fileBefore = store.GetBytes(16, 1);
        var tableBefore = store.GetBytes(255, 16);
        using var container = new Container(CompressionType.Gzip, new MemoryStream([9, 9]));

        Assert.False(cache.TryReplaceContainer(16, 1, original, container, [5, 6, 7, 8]));

        Assert.Equal(fileBefore, store.GetBytes(16, 1));
        Assert.Equal(tableBefore, store.GetBytes(255, 16));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 2, 3, 0)]
    public void TryReplaceContainer_RejectsUnusableKeysBeforeReadingOrWriting(int k0, int k1, int k2, int k3)
    {
        using var store = new MemoryFileStore();
        SeedCache(store, new ReferenceTableCodec(), ArchiveTestData.EncodeArchive([1]));
        using var cache = CreateCache(store);
        var before = store.GetBytes(16, 1);
        using var container = new Container(CompressionType.Gzip, new MemoryStream([9]));

        var keys = k3 == 0 && k0 != 0 ? new[] { k0, k1, k2 } : new[] { k0, k1, k2, k3 };
        Assert.Throws<ArgumentException>(() => cache.TryReplaceContainer(16, 1, [], container, keys));
        Assert.Equal(before, store.GetBytes(16, 1));
    }

    private static byte[] EncryptedGzip(byte[] data, int[] keys)
    {
        using var container = new Container(CompressionType.Gzip, new MemoryStream(data));
        return XteaEncryptor.EncryptXtea(keys, container.Encode(false), 5, container.Encode(false).Length);
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

    private static void SeedCache(MemoryFileStore store, ReferenceTableCodec tableCodec, byte[] archiveBytes, short? version = null, ReferenceTableFlags flags = 0)
    {
        var table = new ReferenceTable(7, 5, flags, 1);
        var entry = new ReferenceTableEntry(1)
        {
            Version = version ?? 4,
            Crc32 = 123
        };
        if (flags.HasFlag(ReferenceTableFlags.Digests)) entry.WhirlpoolDigest = new byte[64];
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(1, entry);

        using var encodedTable = tableCodec.Encode(table);
        store.Set(255, 16, EncodeContainer(encodedTable.ToArray()));
        store.Set(16, 1, EncodeContainer(archiveBytes, version));
    }

    private static byte[] EncodeContainer(byte[] data, short? version = null)
    {
        using var content = new MemoryStream(data);
        using var container = new Container(CompressionType.None, content, version ?? -1);
        return container.Encode();
    }

    private static void AssertTrailerFreeArchive(MemoryFileStore store)
    {
        var containerDecoder = new ContainerDecoder();
        using var stream = new MemoryStream(store.GetBytes(16, 1));
        using var container = containerDecoder.Decode(stream);
        Assert.Equal((short)-1, container.Version);
    }

    private sealed class BlockingChecksumFileStore(MemoryFileStore inner) : IFileStore
    {
        private int _blockChecksumRead;

        public ManualResetEventSlim ChecksumReferenceReadEntered { get; } = new();
        public ManualResetEventSlim ReleaseChecksumReferenceRead { get; } = new();
        public ManualResetEventSlim ArchiveWriteEntered { get; } = new();

        public int IndexFileCount => inner.IndexFileCount;

        public int GetFileCount(int indexId) => inner.GetFileCount(indexId);

        public MemoryStream Read(int indexId, int fileId)
        {
            if (indexId == 255 && fileId == 16 && Interlocked.Exchange(ref _blockChecksumRead, 0) == 1)
            {
                ChecksumReferenceReadEntered.Set();
                if (!ReleaseChecksumReferenceRead.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The checksum-table read was not released.");
                }
            }

            try
            {
                return inner.Read(indexId, fileId);
            }
            catch (FileNotFoundException) when (indexId == 255)
            {
                return new MemoryStream();
            }
        }

        public void Write(int indexId, int fileId, MemoryStream data)
        {
            if (indexId == 16 && fileId == 1)
            {
                ArchiveWriteEntered.Set();
            }

            inner.Write(indexId, fileId, data);
        }

        public void Dispose()
        {
            ChecksumReferenceReadEntered.Dispose();
            ReleaseChecksumReferenceRead.Dispose();
            ArchiveWriteEntered.Dispose();
        }

        public void BlockNextChecksumRead() => Interlocked.Exchange(ref _blockChecksumRead, 1);
    }
}
